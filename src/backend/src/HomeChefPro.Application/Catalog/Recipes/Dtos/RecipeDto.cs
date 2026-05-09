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
    IReadOnlyList<RecipeComponentDto> Components,
    IReadOnlyList<RecipeModifierDto> Modifiers,   // Etapa 2
    IReadOnlyList<string> Tags);                   // Etapa 3

public sealed record RecipeComponentDto(
    Guid Id,
    Guid? IngredientId,
    Guid? SubRecipeId,
    decimal Quantity,
    string? Notes,
    int DisplayOrder);

/// <summary>Etapa 2: opcion de personalizacion del chef para un plato.</summary>
public sealed record RecipeModifierDto(
    Guid Id,
    string Name,
    int DefaultQty,
    int MinQty,
    int MaxQty,
    decimal PriceDeltaUsd,
    int DisplayOrder,
    bool IsActive);

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
    string MenuType,
    IReadOnlyList<string> Tags);  // Etapa 3
