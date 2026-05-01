using FluentValidation;
using MediatR;

namespace HomeChefPro.Application.Common.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{TRequest}"/> for a MediatR request before
/// the handler executes, aggregating failures into an <see cref="Exceptions.ValidationException"/>.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next().ConfigureAwait(false);

        var ctx = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(ctx, cancellationToken))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length > 0)
            throw new Exceptions.ValidationException(failures);

        return await next().ConfigureAwait(false);
    }
}
