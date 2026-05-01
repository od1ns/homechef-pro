using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HomeChefPro.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next().ConfigureAwait(false);
            sw.Stop();
            _logger.LogInformation(
                "MediatR {RequestName} OK in {Elapsed}ms",
                requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex,
                "MediatR {RequestName} FAILED in {Elapsed}ms: {Message}",
                requestName, sw.ElapsedMilliseconds, ex.Message);
            throw;
        }
    }
}
