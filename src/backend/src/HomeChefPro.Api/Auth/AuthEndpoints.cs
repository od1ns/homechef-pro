using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Commands.Login2fa;
using HomeChefPro.Application.Auth.Commands.Totp;
using HomeChefPro.Application.Auth.Commands.ChangePassword;
using HomeChefPro.Application.Auth.Commands.LoginUser;
using HomeChefPro.Application.Auth.Commands.LogoutUser;
using HomeChefPro.Application.Auth.Commands.RefreshAccessToken;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Auth.Queries.GetMe;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        // F-28 (Tier 2): rate limiting granular por endpoint (auth = 10 req/min/IP).
        // Defensa contra brute-force en login/register/refresh.
        var group = app.MapGroup("/api/auth").WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", async (
            [FromBody] RegisterUserCommand cmd,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // F-21 (audit Pasada A, BOPLA / API3:2023): nunca confiar el campo `Roles`
            // venido del body. Si se aceptara, cualquier anonimo podria auto-registrarse
            // como Admin. El handler asigna Client por default cuando Roles es null.
            // La asignacion de roles privilegiados ocurre via IIdentityService.AssignRoleAsync
            // en flujos administrativos (no expuestos a HTTP anonymous).
            // Sesion A: el endpoint setea IP + User-Agent para audit del invitation use.
            var safeCmd = cmd with
            {
                Roles = null,
                UserIp = ctx.Connection.RemoteIpAddress?.ToString(),
                UserAgent = ctx.Request.Headers.UserAgent.ToString(),
            };
            var result = await mediator.Send(safeCmd, ct);
            return Results.Created($"/api/auth/users/{result.UserId}", result);
        })
        .AllowAnonymous()
        .WithName("Register")
        .Produces<AuthResultDto>(StatusCodes.Status201Created);

        group.MapPost("/login", async (
            [FromBody] LoginUserCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Login")
        .Produces<AuthResultDto>();

        // Intercambia un refresh token por un nuevo par (access + refresh).
        group.MapPost("/refresh", async (
            [FromBody] RefreshAccessTokenCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("RefreshAccessToken")
        .Produces<AuthResultDto>();

        // F-16 (Tier 2): logout requiere autenticacion.
        // Antes era AllowAnonymous y un atacante con un refresh token robado podia
        // revocarlo (DoS al usuario legitimo) sin necesidad de credenciales.
        // Ahora exige access token valido + refresh en el body.
        group.MapPost("/logout", async (
            [FromBody] LogoutUserCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Logout");

        group.MapPost("/change-password", async (
            [FromBody] ChangePasswordCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("ChangePassword");

        group.MapGet("/me", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var me = await mediator.Send(new GetMeQuery(), ct);
            return Results.Ok(me);
        })
        .RequireAuthorization()
        .WithName("GetMe")
        .Produces<UserSummaryDto>();

        // ===================================================================
        // F-17 (Tier 3): MFA TOTP. Flujo:
        //   1) POST /api/auth/2fa/setup     -> retorna URI otpauth (QR)
        //   2) POST /api/auth/2fa/verify-setup -> activa 2FA con primer codigo
        //   3) POST /api/auth/login         -> ahora retorna { requires2fa: true, partialToken }
        //   4) POST /api/auth/2fa/login     -> intercambia partial + codigo por JWT real
        //   5) POST /api/auth/2fa/disable   -> desactiva (requiere codigo)
        // ===================================================================

        group.MapPost("/2fa/setup", async (
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new SetupTotpCommand(), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("Setup2fa")
        .Produces<TotpSetupResult>();

        group.MapPost("/2fa/verify-setup", async (
            [FromBody] VerifyTotpSetupCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("VerifySetup2fa");

        group.MapPost("/2fa/disable", async (
            [FromBody] DisableTotpCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithName("Disable2fa");

        group.MapPost("/2fa/login", async (
            [FromBody] Login2faCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Ok(result);
        })
        .AllowAnonymous()
        .WithName("Login2fa")
        .Produces<AuthResultDto>();

        return app;
    }
}
