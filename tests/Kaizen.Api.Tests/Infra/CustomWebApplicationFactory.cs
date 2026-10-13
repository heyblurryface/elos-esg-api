using Kaizen.Api.Data;
using Kaizen.Api.Services.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kaizen.Api.Tests.Infra;

/// <summary>
/// Sobe a API em memória para os testes de integração:
///  - força o ambiente "Testing" (o Program.cs não registra Oracle nem roda migração);
///  - registra o KaizenDbContext usando o provider InMemory;
///  - substitui a autenticação JWT pelo <see cref="TestAuthHandler"/>;
///  - popula o banco em memória com os dados de demonstração via DbSeeder.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Cada instância da factory (uma por classe de teste) recebe um banco próprio,
    // evitando que classes de teste rodando em paralelo compartilhem o mesmo store
    // e colidam ao semear os dados (erro "An item with the same key has already been added").
    private readonly string _dbName = "KaizenTestDb_" + Guid.NewGuid();

    // Root compartilhado: garante que o provider temporário usado no seed e o provider
    // real usado pelos requests HTTP enxerguem exatamente o mesmo banco em memória.
    private static readonly InMemoryDatabaseRoot DbRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove qualquer registro anterior do DbContext (defensivo).
            services.RemoveAll(typeof(DbContextOptions<KaizenDbContext>));
            services.RemoveAll(typeof(KaizenDbContext));

            services.AddDbContext<KaizenDbContext>(options =>
                options.UseInMemoryDatabase(_dbName, DbRoot));

            // Autenticação de teste: torna "Test" o esquema padrão.
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                options.DefaultScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Popula o banco em memória.
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KaizenDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            db.Database.EnsureCreated();
            DbSeeder.SeedAsync(db, hasher).GetAwaiter().GetResult();
        });
    }
}
