namespace HomeChefPro.Application.Catalog.Recipes.Dtos;

public sealed record RecipeDto(
    Guid Id,
    string Name,
    string? Description,
    string? Category,
    bool IsSubRecipe,
    string? ProcedureMarkdown,
    decimal? YieldQuantity,
    string? YieldUnit,
    decimal? SuggestedPriceUsd,
    decimal? SellingPriceUsd,
    int PrepTimeMinutes,
    string? ImageUrl,
    bool IsActive,
    bool IsOutOfStock,
    string MenuType,
    DateTimeOffset? SpecialFrom,
    DateTimeOffset? SpecialTo,
    IReadOnlyList<RecipeComponentDto> Components);

public sealed record RecipeComponentDto(
    Guid Id,
    Guid? IngredientId,
    Guid? SubRecipeId,
    decimal Quantity,
    string? Notes,
    int DisplayOrder);

public sealed record RecipeSummaryDto(
    Guid Id,
    string Name,
    string? Category,
    bool IsSubRecipe,
    decimal? SellingPriceUsd,
    int PrepTimeMinutes,
    string? ImageUrl,
    bool IsActive,
    bool IsOutOfStock,
    string MenuType);
