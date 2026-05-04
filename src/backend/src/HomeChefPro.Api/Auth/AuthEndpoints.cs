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
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (
            [FromBody] RegisterUserCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            // F-21 (audit Pasada A, BOPLA / API3:2023): nunca confiar el campo `Roles`
            // venido del body. Si se aceptara, cualquier anonimo podria auto-registrarse
            // como Admin. El handler asigna Client por default cuando Roles es null.
            // La asignacion de roles privilegiados ocurre via IIdentityService.AssignRoleAsync
            // en flujos administrativos (no expuestos a HTTP anonymous).
            var safeCmd = cmd with { Roles = null };
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

        // Revoca el refresh token actual. El access token sigue vigente
        // hasta su expiracion natural.
        group.MapPost("/logout", async (
            [FromBody] LogoutUserCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(cmd, ct);
            return Results.NoContent();
        })
        .AllowAnonymous()
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

        return app;
    }
}
