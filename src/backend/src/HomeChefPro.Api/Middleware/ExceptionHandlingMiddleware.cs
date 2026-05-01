using System.Net;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            await WriteProblemAsync(context, HttpStatusCode.InternalServerError,
                title: "Internal server error",
                detail: "An unexpected error occurred.");
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
