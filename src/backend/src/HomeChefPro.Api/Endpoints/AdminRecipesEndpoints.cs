using HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.AddSubRecipeComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateSubRecipe;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateRecipeModifier;
using HomeChefPro.Application.Catalog.Recipes.Commands.UpdateRecipeModifier;
using HomeChefPro.Application.Catalog.Recipes.Commands.DeleteRecipeModifier;
using HomeChefPro.Application.Catalog.Recipes.Commands.ToggleOutOfStock;
using HomeChefPro.Application.Catalog.Recipes.Commands.UpdateSellingPrice;
using HomeChefPro.Application.Catalog.Recipes.Commands.UpdateRecipeImage;
using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipe;
using HomeChefPro.Application.Uploads.Abstractions;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.Extensions.Options;
using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipeCost;
using HomeChefPro.Application.Catalog.Recipes.Queries.ListRecipes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminRecipesEndpoints
{
    public static IEndpointRouteBuilder MapAdminRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/recipes")
            .WithTags("Admin: Recipes")
            .RequireAuthorization("Admin");

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeSubRecipes = false,
            [FromQuery] bool onlyActive = true,
            [FromQuery] bool onlyOnMenu = false,
            [FromQuery] string? menuType = null,
            [FromQuery] string? search = null) =>
        {
            var list = await mediator.Send(new ListRecipesQuery(
                includeSubRecipes, onlyActive, onlyOnMenu, menuType, search), ct);
            return Results.Ok(list);
        });

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetRecipeQuery(id), ct)));

        group.MapGet("{id:guid}/cost", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetRecipeCostQuery(id), ct)));

        group.MapPost("dishes", async (
            [FromBody] CreateDishCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/recipes/{id}", id);
        });

        group.MapPost("sub-recipes", async (
            [FromBody] CreateSubRecipeCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/recipes/{id}", id);
        });

        group.MapPost("{id:guid}/components/ingredient", async (
            Guid id,
            [FromBody] AddIngredientComponentRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var componentId = await mediator.Send(new AddIngredientComponentCommand(
                RecipeId: id,
                IngredientId: body.IngredientId,
                Quantity: body.Quantity,
                Notes: body.Notes,
                DisplayOrder: body.DisplayOrder), ct);
            return EndpointResults.CreatedId(
                $"/api/admin/recipes/{id}/components/{componentId}", componentId);
        });

        group.MapPost("{id:guid}/components/sub-recipe", async (
            Guid id,
            [FromBody] AddSubRecipeComponentRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var componentId = await mediator.Send(new AddSubRecipeComponentCommand(
                RecipeId: id,
                SubRecipeId: body.SubRecipeId,
                Quantity: body.Quantity,
                Notes: body.Notes,
                DisplayOrder: body.DisplayOrder), ct);
            return EndpointResults.CreatedId(
                $"/api/admin/recipes/{id}/components/{componentId}", componentId);
        });

        group.MapPatch("{id:guid}/selling-price", async (
            Guid id,
            [FromBody] UpdateSellingPriceRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new UpdateSellingPriceCommand(id, body.SellingPriceUsd), ct);
            return Results.NoContent();
        });

        group.MapPost("{id:guid}/out-of-stock", async (
            Guid id,
            [FromBody] ToggleOutOfStockRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ToggleOutOfStockCommand(id, body.OutOfStock), ct);
            return Results.NoContent();
        });

        // Etapa 1: subir + asociar imagen del recipe en una sola operacion.
        // Reusa storage local. Multipart/form-data con field "file".
        // Valida content-type + magic bytes (defense in depth, igual a F-09).
        group.MapPost("{id:guid}/image", async (
            Guid id,
            HttpRequest request,
            IFileStorage storage,
            IUploadUrlBuilder urlBuilder,
            IOptions<LocalFileStorageOptions> opts,
            IMediator mediator,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data." });

            var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded under field 'file'." });
            if (file.Length > opts.Value.MaxBytes)
                return Results.BadRequest(new { error = $"File exceeds limit of {opts.Value.MaxBytes / 1024} KB." });

            var contentType = file.ContentType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(contentType) || !opts.Value.AllowedContentTypes.Contains(contentType))
                return Results.BadRequest(new { error = $"Content-Type '{contentType}' is not allowed.", allowed = opts.Value.AllowedContentTypes });

            // F-09: magic bytes
            byte[] header = new byte[12];
            int read;
            await using (var probe = file.OpenReadStream())
            {
                read = 0;
                while (read < header.Length)
                {
                    var n = await probe.ReadAsync(header.AsMemory(read, header.Length - read), ct).ConfigureAwait(false);
                    if (n == 0) break;
                    read += n;
                }
            }
            if (read < 3 || !MagicBytesMatch(header.AsSpan(0, read), contentType))
                return Results.BadRequest(new { error = "File contents do not match a supported image format (JPEG/PNG/WebP)." });

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext))
                ext = contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", _ => ".bin" };
            var filename = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";

            var chefId = HomeChefPro.Domain.Tenancy.Chef.PilotoId; // single-tenant default
            await using var stream = file.OpenReadStream();
            await storage.SaveAsync(
                chefId: chefId,
                folder: "recipes",
                filename: filename,
                content: stream,
                contentType: contentType,
                ct: ct).ConfigureAwait(false);

            var imageUrl = urlBuilder.BuildRecipeImageUrl(chefId, filename);
            await mediator.Send(new UpdateRecipeImageCommand(id, imageUrl), ct).ConfigureAwait(false);
            return Results.Ok(new { imageUrl });
        })
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .WithName("UploadRecipeImage");

        // ── Etapa 2: Modificadores de receta ──────────────────────────────

        group.MapPost("{id:guid}/modifiers", async (
            Guid id,
            [FromBody] CreateModifierRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var modifierId = await mediator.Send(new CreateRecipeModifierCommand(
                RecipeId: id,
                Name: body.Name,
                DefaultQty: body.DefaultQty,
                MinQty: body.MinQty,
                MaxQty: body.MaxQty,
                PriceDeltaUsd: body.PriceDeltaUsd,
                DisplayOrder: body.DisplayOrder), ct);
            return EndpointResults.CreatedId(
                $"/api/admin/recipes/{id}/modifiers/{modifierId}", modifierId);
        });

        group.MapPut("{id:guid}/modifiers/{modifierId:guid}", async (
            Guid id,
            Guid modifierId,
            [FromBody] UpdateModifierRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new UpdateRecipeModifierCommand(
                RecipeId: id,
                ModifierId: modifierId,
                Name: body.Name,
                DefaultQty: body.DefaultQty,
                MinQty: body.MinQty,
                MaxQty: body.MaxQty,
                PriceDeltaUsd: body.PriceDeltaUsd,
                DisplayOrder: body.DisplayOrder), ct);
            return Results.NoContent();
        });

        group.MapDelete("{id:guid}/modifiers/{modifierId:guid}", async (
            Guid id,
            Guid modifierId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new DeleteRecipeModifierCommand(id, modifierId), ct);
            return Results.NoContent();
        });

        return app;
    }

    // F-09: magic bytes validation (copy de UploadEndpoints; refactor a shared helper queda para Etapa 3).
    private static bool MagicBytesMatch(ReadOnlySpan<byte> buf, string contentType)
    {
        if (contentType == "image/jpeg")
            return buf.Length >= 3 && buf[0] == 0xFF && buf[1] == 0xD8 && buf[2] == 0xFF;
        if (contentType == "image/png")
            return buf.Length >= 8
                && buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E && buf[3] == 0x47
                && buf[4] == 0x0D && buf[5] == 0x0A && buf[6] == 0x1A && buf[7] == 0x0A;
        if (contentType == "image/webp")
            return buf.Length >= 12
                && buf[0] == 0x52 && buf[1] == 0x49 && buf[2] == 0x46 && buf[3] == 0x46
                && buf[8] == 0x57 && buf[9] == 0x45 && buf[10] == 0x42 && buf[11] == 0x50;
        return false;
    }

    public sealed record AddIngredientComponentRequest(
        Guid IngredientId, decimal Quantity, string? Notes = null, int DisplayOrder = 0);

    public sealed record AddSubRecipeComponentRequest(
        Guid SubRecipeId, decimal Quantity, string? Notes = null, int DisplayOrder = 0);

    public sealed record UpdateSellingPriceRequest(decimal SellingPriceUsd);
    public sealed record ToggleOutOfStockRequest(bool OutOfStock);

    // Etapa 2: requests de modificadores
    public sealed record CreateModifierRequest(
        string Name,
        int DefaultQty = 0,
        int MinQty = 0,
        int MaxQty = 1,
        decimal PriceDeltaUsd = 0m,
        int DisplayOrder = 0);

    public sealed record UpdateModifierRequest(
        string Name,
        int DefaultQty,
        int MinQty,
        int MaxQty,
        decimal PriceDeltaUsd,
        int DisplayOrder);
}
