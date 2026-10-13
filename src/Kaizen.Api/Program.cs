using System.Text;
using System.Text.Json;
using Kaizen.Api.Common.Middleware;
using Kaizen.Api.Data;
using Kaizen.Api.Services;
using Kaizen.Api.Services.Abstractions;
using Kaizen.Api.Services.Auth;
using Kaizen.Api.Services.Diagnostico;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// EF Core / Oracle
// Em ambiente "Testing" o DbContext é trocado por InMemory pela
// CustomWebApplicationFactory, então aqui só registramos o provider Oracle
// quando NÃO estamos testando.
// ----------------------------------------------------------------------------
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("OracleConnection")
        ?? throw new InvalidOperationException("Connection string 'OracleConnection' não configurada.");

    builder.Services.AddDbContext<KaizenDbContext>(options =>
        options.UseOracle(connectionString));
}

// ----------------------------------------------------------------------------
// Opções de JWT (seção "Jwt" do appsettings)
// ----------------------------------------------------------------------------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey.Length >= 32
                    ? jwtOptions.SecretKey
                    : jwtOptions.SecretKey.PadRight(32, '0'))),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ----------------------------------------------------------------------------
// Injeção de dependências dos serviços de domínio
// ----------------------------------------------------------------------------
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IConsultorService, ConsultorService>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();
builder.Services.AddScoped<IAvaliacaoService, AvaliacaoService>();
builder.Services.AddScoped<IRespostaService, RespostaService>();
builder.Services.AddScoped<IResultadoService, ResultadoService>();
builder.Services.AddScoped<IRecomendacaoService, RecomendacaoService>();
builder.Services.AddScoped<IDiagnosticoEngine, DiagnosticoEngine>();

// ----------------------------------------------------------------------------
// Controllers + validação. Resposta 400 padronizada como ProblemDetails.
// ----------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Title = "Erro de validação",
            Status = StatusCodes.Status400BadRequest,
            Detail = "Um ou mais campos estão inválidos.",
            Instance = context.HttpContext.Request.Path
        };
        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

// ----------------------------------------------------------------------------
// Swagger / OpenAPI com suporte a Bearer token
// ----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Kaizen API",
        Version = "v1",
        Description = "API de diagnóstico de maturidade em Governança & Compliance (ESG). " +
                      "Reimplementação em ASP.NET Core 8 da plataforma Kaizen (FIAP)."
    });

    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe o token JWT obtido em /api/auth/login. Ex.: 'Bearer {token}'.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ----------------------------------------------------------------------------
// Pipeline HTTP
// ----------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kaizen API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ----------------------------------------------------------------------------
// Health check (DevOps): usado pelo healthcheck do Docker, pelo smoke test do
// pipeline e para evidenciar em qual ambiente/versão a API está rodando.
// Público de propósito: não expõe dados, só o estado da aplicação e do banco.
// ----------------------------------------------------------------------------
app.MapGet("/health", async (KaizenDbContext db, IWebHostEnvironment env, IConfiguration config) =>
    {
        bool dbOk;
        try
        {
            dbOk = await db.Database.CanConnectAsync();
        }
        catch
        {
            dbOk = false;
        }

        var payload = new
        {
            status = dbOk ? "Healthy" : "Unhealthy",
            application = "Elos ESG API (Kaizen)",
            environment = env.EnvironmentName,
            version = config["APP_VERSION"] ?? "local",
            database = dbOk ? "Up" : "Down",
            timestampUtc = DateTime.UtcNow
        };

        return dbOk
            ? Results.Ok(payload)
            : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
    })
    .AllowAnonymous()
    .WithTags("Health");

// Raiz redireciona para o Swagger (facilita abrir o ambiente pelo navegador).
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// ----------------------------------------------------------------------------
// Migração + seed (apenas fora de Testing). Retry para esperar o banco subir
// no docker-compose.
// ----------------------------------------------------------------------------
if (!app.Environment.IsEnvironment("Testing"))
{
    await MigrateAndSeedAsync(app);
}

app.Run();

static async Task MigrateAndSeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<KaizenDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            logger.LogInformation("Aplicando migrações (tentativa {Attempt}/{Max})...", attempt, maxAttempts);
            await db.Database.MigrateAsync();
            await DbSeeder.SeedAsync(db, hasher);
            logger.LogInformation("Banco migrado e populado com sucesso.");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Banco indisponível na tentativa {Attempt}. Aguardando...", attempt);
            if (attempt == maxAttempts) throw;
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }
}

// Necessário para o WebApplicationFactory<Program> nos testes de integração.
public partial class Program { }
