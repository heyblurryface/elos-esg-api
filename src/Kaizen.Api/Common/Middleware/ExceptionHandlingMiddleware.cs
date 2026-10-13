using System.Text.Json;
using Kaizen.Api.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kaizen.Api.Common.Middleware;

/// <summary>
/// Tratamento global de exceções: converte exceções de domínio e erros inesperados
/// em respostas ProblemDetails (RFC 7807) padronizadas.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.ErrorTitle, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Erro interno", "Ocorreu um erro inesperado ao processar a requisição.");
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        if (ctx.Response.HasStarted) return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = ctx.Request.Path
        };

        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
