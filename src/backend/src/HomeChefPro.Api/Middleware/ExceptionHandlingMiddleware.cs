using System.Net;
using System.Text;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment env)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;
    private readonly IHostEnvironment _env = env;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation("Validation failure on {Path}: {Count} errors", context.Request.Path, ex.Errors.Count);
            await WriteProblemAsync(context, HttpStatusCode.BadRequest,
                title: "Validation failed",
                detail: ex.Message,
                extensions: new Dictionary<string, object?> { ["errors"] = ex.Errors });
        }
        catch (NotFoundException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.NotFound,
                title: "Not found",
                detail: ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Conflict,
                title: "Domain rule violated",
                detail: ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblemAsync(context, HttpStatusCode.Unauthorized,
                title: "Unauthorized",
                detail: ex.Message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // F-26 (Tier 2): otra request modifico la misma fila entre nuestro SELECT
            // y nuestro UPDATE. El cliente debe re-leer el state actual y reintentar.
            _logger.LogInformation("Concurrency conflict on {Path}: {Entries} entries", context.Request.Path, ex.Entries.Count);
            await WriteProblemAsync(context, HttpStatusCode.Conflict,
                title: "Concurrent modification",
                detail: "Otra operacion modifico este recurso. Recarga y reintenta.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            // En Development incluimos el mensaje real de la excepcion (y
            // toda la cadena de inner exceptions) para facilitar debugging
            // desde tests E2E o desde el navegador. En Production solo el
            // mensaje generico para no filtrar internals.
            string detail;
            if (_env.IsDevelopment())
            {
                var sb = new StringBuilder();
                var current = ex;
                var depth = 0;
                while (current is not null && depth < 5)
                {
                    if (depth > 0) sb.Append(" --> ");
                    sb.Append(current.GetType().Name).Append(": ").Append(current.Message);
                    current = current.InnerException;
                    depth++;
                }
                detail = sb.ToString();
            }
            else
            {
                detail = "An unexpected error occurred.";
            }
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError,
                title: "Internal server error",
                detail: detail);
        }
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        HttpStatusCode status,
        string title,
        string detail,
        IDictionary<string, object?>? extensions = null)
    {
        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Type = $"https://httpstatuses.io/{(int)status}",
            Title = title,
            Status = (int)status,
            Detail = detail,
            Instance = context.Request.Path,
        };
        if (extensions is not null)
            foreach (var (k, v) in extensions)
                problem.Extensions[k] = v;

        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.Status,
            type: problem.Type,
            instance: problem.Instance,
            extensions: problem.Extensions).ExecuteAsync(context);
    }
}
