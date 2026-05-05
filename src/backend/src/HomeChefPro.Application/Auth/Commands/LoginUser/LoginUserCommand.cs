using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Auth.Services;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Auth.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password) : IRequest<AuthResultDto>;

public sealed class LoginUserValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginUserHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    IJwtTokenService jwt,
    RefreshTokenIssuer refreshIssuer)
    : IRequestHandler<LoginUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginUserCommand request, CancellationToken ct)
    {
        var attempt = await identity.VerifyPasswordAsync(request.Email, request.Password, ct)
            .ConfigureAwait(false);
        if (!attempt.Succeeded || attempt.UserId is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == attempt.UserId.Value, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(HomeChefPro.Domain.Identity.UserProfile), attempt.UserId.Value);

        // Pasada C / Fase 1C-A: en single-tenant todos los users apuntan al piloto.
        // Fase 2: leer chef_staff(chef_id, user_id) o equivalente.
        var token = jwt.Issue(
            userId: attempt.UserId.Value,
            chefId: HomeChefPro.Domain.Tenancy.Chef.PilotoId,
            email: attempt.Email!,
            fullName: profile.FullName,
            roles: attempt.Roles);
        var refresh = await refreshIssuer.IssueAndPersistAsync(attempt.UserId.Value, ct: ct)
            .ConfigureAwait(false);

        return new AuthResultDto(
            UserId: attempt.UserId.Value,
            Email: attempt.Email!,
            FullName: profile.FullName,
            Roles: [.. attempt.Roles],
            AccessToken: token.AccessToken,
            ExpiresAt: token.ExpiresAt,
            RefreshToken: refresh.PlainToken,
            RefreshExpiresAt: refresh.ExpiresAt);
    }
}
