using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Auth.Services;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string FullName,
    string? Phone = null,
    string PreferredLanguage = "es-VE",
    IReadOnlyCollection<string>? Roles = null,
    // Sesion A / Frente 1: codigo de invitacion (requerido si config exige).
    string? InvitationCode = null,
    // Audit metadata: lo settea el endpoint.
    string? UserIp = null,
    string? UserAgent = null) : IRequest<AuthResultDto>;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Phone).MaximumLength(30);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(10);
        RuleFor(x => x.InvitationCode).MaximumLength(32);
    }
}

public sealed class RegisterUserHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    IJwtTokenService jwt,
    RefreshTokenIssuer refreshIssuer,
    Microsoft.Extensions.Configuration.IConfiguration configuration,
    TimeProvider clock)
    : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken ct)
    {
        var userId = Guid.NewGuid();
        var roles = request.Roles is { Count: > 0 } ? request.Roles : new[] { Roles.Client };
        var now = clock.GetUtcNow();

        // Sesion A / Frente 1: validar codigo de invitacion ANTES de crear el user.
        // Bootstrap:RequireInvitationCode=true por default — el operador puede
        // setearlo a false en dev/staging si quiere registro libre.
        // Usamos AsNoTracking en el lookup para no contaminar el ChangeTracker
        // (Identity hace sus propios SaveChanges y mezcla con tracking nuestro
        // generaba DbUpdateConcurrencyException).
        var requireCodeRaw = configuration["Bootstrap:RequireInvitationCode"];
        var requireCode = !bool.TryParse(requireCodeRaw, out var b) || b; // default true
        Guid? invitationId = null;
        if (requireCode)
        {
            if (string.IsNullOrWhiteSpace(request.InvitationCode))
            {
                throw new Common.Exceptions.ValidationException(
                    [new FluentValidation.Results.ValidationFailure(
                        nameof(RegisterUserCommand.InvitationCode),
                        "Invitation code is required to register.")]);
            }
            var codeNorm = request.InvitationCode.Trim();
            var snapshot = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(
                    db.InvitationCodes.AsNoTracking().Where(i => i.Code == codeNorm),
                    ct).ConfigureAwait(false);
            if (snapshot is null || !snapshot.IsActive(now))
            {
                throw new Common.Exceptions.ValidationException(
                    [new FluentValidation.Results.ValidationFailure(
                        nameof(RegisterUserCommand.InvitationCode),
                        "Invitation code is invalid, revoked, expired, or already used.")]);
            }
            invitationId = snapshot.Id;
        }

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

        // Identity persistio el user con su propio SaveChanges interno y lo dejo
        // tracked. Limpiamos para que nuestros SaveChanges siguientes no
        // disparen DbUpdateConcurrencyException por entities residuales
        // (Identity tracked roles + tokens al hacer AddToRolesAsync).
        if (db is Microsoft.EntityFrameworkCore.DbContext ctx)
            ctx.ChangeTracker.Clear();

        var profile = UserProfile.Create(
            userId: userId,
            fullName: request.FullName,
            defaultPhone: request.Phone,
            preferredLanguage: request.PreferredLanguage,
            clock: clock);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Sesion A: consumir el codigo via UPDATE atomico (ExecuteUpdateAsync),
        // sin pasar por ChangeTracker. Eso evita conflicts con tracking residual
        // de Identity (ConcurrencyStamp en AspNetUsers).
        if (invitationId is { } invId)
        {
            if (db is Microsoft.EntityFrameworkCore.DbContext ctx2)
                ctx2.ChangeTracker.Clear();

            // UPDATE WHERE codigo aun valido + used_count < max_uses.
            // Si rowsAffected == 0, alguien consumio entre nuestro snapshot y aqui.
            var rowsAffected = await db.InvitationCodes
                .Where(i => i.Id == invId
                            && i.RevokedAt == null
                            && (i.ExpiresAt == null || i.ExpiresAt > now)
                            && i.UsedCount < i.MaxUses)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(i => i.UsedCount, i => i.UsedCount + 1),
                    ct).ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                throw new Common.Exceptions.ValidationException(
                    [new FluentValidation.Results.ValidationFailure(
                        nameof(RegisterUserCommand.InvitationCode),
                        "Invitation code was just consumed by another registration. Try a new code.")]);
            }

            // Crear audit record del uso (tabla independiente, sin tracking issues).
            var use = HomeChefPro.Domain.Invitations.InvitationCodeUse.Create(
                invId, userId, now, request.UserIp, request.UserAgent);
            db.InvitationCodeUses.Add(use);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

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
