using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Domain.Identity;
using MediatR;

namespace HomeChefPro.Application.Customers.Commands.PutMyPreferences;

public sealed record PutMyPreferencesCommand(string PayloadJson) : IRequest;

public sealed class PutMyPreferencesValidator : AbstractValidator<PutMyPreferencesCommand>
{
    public PutMyPreferencesValidator()
    {
        // Cap at ~64KB so a misbehaving client cannot dump a giant blob into the DB.
        RuleFor(x => x.PayloadJson).NotEmpty().MaximumLength(64 * 1024);
    }
}

public sealed class PutMyPreferencesHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<PutMyPreferencesCommand>
{
    public async Task Handle(PutMyPreferencesCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var existing = await db.CustomerPreferences.FindAsync([userId], ct).ConfigureAwait(false);
        if (existing is null)
        {
            db.CustomerPreferences.Add(new CustomerPreferences(
                userId, request.PayloadJson, clock.GetUtcNow()));
        }
        else
        {
            existing.Replace(request.PayloadJson, clock);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
