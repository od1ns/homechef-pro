using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Auth.Services;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Identity;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string? Phone = null,
    string PreferredLanguage = "es-VE",
    IReadOnlyCollection<string>? Roles = null) : IRequest<AuthResultDto>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(10);
    }
}

public sealed class RegisterUserHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    IJwtTokenService jwt,
    RefreshTokenIssuer refreshIssuer,
    TimeProvider clock)
    : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var userId = Guid.NewGuid();
        var roles = request.Roles is { Count: > 0 } ? request.Roles : new[] { Roles.Client };

        await identity.EnsureRolesExistAsync(roles, ct).ConfigureAwait(false);

        var op = await identity.CreateUserAsync(
            userId: userId,
            email: request.Email,
            password: request.Password,
            phone: request.Phone,
            roles: roles,
            ct: ct).ConfigureAwait(false);

        if (!op.Succeeded)
            throw new Common.Exceptions.ValidationException(
                op.Errors.Select(e => new FluentValidation.Results.ValidationFailure("Identity", e)));

        var profile = UserProfile.Create(
            userId: userId,
            fullName: request.FullName,
            defaultPhone: request.Phone,
            preferredLanguage: request.PreferredLanguage,
            clock: clock);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Pasada C / Fase 1C-A: nuevos registros siempre asignados al piloto en
        // single-tenant. Fase 2: el ChefId se determina por el flow (registro
        // staff exige un chef de invitacion; cliente puede crear su propio chef).
        var token = jwt.Issue(
            userId: userId,
            chefId: HomeChefPro.Domain.Tenancy.Chef.PilotoId,
            email: request.Email,
            fullName: request.FullName,
            roles: [.. roles]);
        var refresh = await refreshIssuer.IssueAndPersistAsync(userId, ct: ct).ConfigureAwait(false);

        return new AuthResultDto(
            UserId: userId,
            Email: request.Email,
            FullName: request.FullName,
            Roles: [.. roles],
            AccessToken: token.AccessToken,
            ExpiresAt: token.ExpiresAt,
            RefreshToken: refresh.PlainToken,
            RefreshExpiresAt: refresh.ExpiresAt);
    }
}
