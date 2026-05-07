using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Invitations;
using MediatR;

namespace HomeChefPro.Application.Invitations.Commands.CreateInvitation;

public sealed record CreateInvitationCommand(
    Guid? ChefId = null,
    DateTimeOffset? ExpiresAt = null,
    int MaxUses = 1,
    string? Notes = null,
    string? CustomCode = null) : IRequest<InvitationCodeDto>;

public sealed record InvitationCodeDto(
    Guid Id,
    string Code,
    Guid? ChefId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    int MaxUses,
    int UsedCount,
    bool IsActive,
    DateTimeOffset? RevokedAt,
    string? Notes);

public sealed class CreateInvitationValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationValidator()
    {
        RuleFor(x => x.MaxUses).GreaterThan(0).LessThanOrEqualTo(10000);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.CustomCode).MaximumLength(32);
        When(x => x.CustomCode is not null, () =>
        {
            RuleFor(x => x.CustomCode).Matches(@"^[A-Za-z0-9-_]+$")
                .WithMessage("Custom code only allows letters, digits, hyphen, underscore.");
        });
    }
}

public sealed class CreateInvitationHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<CreateInvitationCommand, InvitationCodeDto>
{
    public async Task<InvitationCodeDto> Handle(CreateInvitationCommand request, CancellationToken ct)
    {
        var creatorId = currentUser.RequireUserId();
        var now = clock.GetUtcNow();

        var inv = InvitationCode.Create(
            createdBy: creatorId,
            now: now,
            chefId: request.ChefId,
            expiresAt: request.ExpiresAt,
            maxUses: request.MaxUses,
            notes: request.Notes,
            customCode: request.CustomCode);

        db.InvitationCodes.Add(inv);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return ToDto(inv, now);
    }

    internal static InvitationCodeDto ToDto(InvitationCode i, DateTimeOffset now) => new(
        Id: i.Id,
        Code: i.Code,
        ChefId: i.ChefId,
        CreatedAt: i.CreatedAt,
        ExpiresAt: i.ExpiresAt,
        MaxUses: i.MaxUses,
        UsedCount: i.UsedCount,
        IsActive: i.IsActive(now),
        RevokedAt: i.RevokedAt,
        Notes: i.Notes);
}
