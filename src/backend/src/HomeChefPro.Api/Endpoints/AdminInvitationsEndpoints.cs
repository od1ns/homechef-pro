using HomeChefPro.Application.Invitations.Commands.CreateInvitation;
using HomeChefPro.Application.Invitations.Commands.RevokeInvitation;
using HomeChefPro.Application.Invitations.Queries.ListInvitations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminInvitationsEndpoints
{
    public static IEndpointRouteBuilder MapAdminInvitationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/invitations").WithTags("Admin: Invitations");

        // POST /api/admin/invitations -> generar codigo
        group.MapPost("", async (
            [FromBody] CreateInvitationCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Created($"/api/admin/invitations/{result.Id}", result);
        })
        .RequireAuthorization("Admin")
        .WithName("CreateInvitation")
        .Produces<InvitationCodeDto>(StatusCodes.Status201Created);

        // GET /api/admin/invitations?onlyActive=true&chefId=...&pageSize=50
        group.MapGet("", async (
            bool onlyActive,
            Guid? chefId,
            int? pageSize,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var q = new ListInvitationsQuery(
                OnlyActive: onlyActive,
                ChefId: chefId,
                PageSize: pageSize ?? 50);
            var list = await mediator.Send(q, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization("Admin")
        .WithName("ListInvitations");

        // POST /api/admin/invitations/{id}/revoke
        group.MapPost("{id:guid}/revoke", async (
            Guid id,
            [FromBody] RevokeInvitationBody body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RevokeInvitationCommand(id, body?.Reason), ct);
            return Results.NoContent();
        })
        .RequireAuthorization("Admin")
        .WithName("RevokeInvitation");

        return app;
    }

    public sealed record RevokeInvitationBody(string? Reason);
}
