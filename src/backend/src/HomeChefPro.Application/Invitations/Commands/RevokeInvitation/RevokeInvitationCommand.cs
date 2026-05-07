using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Invitations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Invitations.Commands.RevokeInvitation;

public sealed record RevokeInvitationCommand(Guid Id, string? Reason = null) : IRequest<Unit>;

public sealed class RevokeInvitationValidator : AbstractValidator<RevokeInvitationCommand>
{
    public RevokeInvitationValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(200);
    }
}

public sealed class RevokeInvitationHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<RevokeInvitationCommand, Unit>
{
    public async Task<Unit> Handle(RevokeInvitationCommand request, CancellationToken ct)
    {
        var inv = await db.InvitationCodes
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(InvitationCode), request.Id);

        inv.Revoke(currentUser.RequireUserId(), clock.GetUtcNow(), request.Reason);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
