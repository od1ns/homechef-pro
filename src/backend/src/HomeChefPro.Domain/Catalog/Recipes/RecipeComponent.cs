using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes;

public sealed class RecipeComponent : Entity<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public Guid ParentRecipeId { get; private set; }
    public Guid? IngredientId { get; private set; }
    public Guid? SubRecipeId { get; private set; }
    public decimal Quantity { get; private set; }
    public string? Notes { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public bool IsIngredient => IngredientId is not null;
    public bool IsSubRecipe => SubRecipeId is not null;

    private RecipeComponent() { }

    private RecipeComponent(
        Guid id,
        Guid parentRecipeId,
        Guid? ingredientId,
        Guid? subRecipeId,
        decimal quantity,
        string? notes,
        int displayOrder,
        DateTimeOffset now)
    {
        Id = id;
        ParentRecipeId = parentRecipeId;
        IngredientId = ingredientId;
        SubRecipeId = subRecipeId;
        Quantity = quantity;
        Notes = notes;
        DisplayOrder = displayOrder;
        CreatedAt = now;
    }

    public static RecipeComponent ForIngredient(
        Guid parentRecipeId,
        Guid ingredientId,
        decimal quantity,
        string? notes,
        int displayOrder,
        TimeProvider? clock,
        Guid? id)
    {
        ValidateQuantity(quantity);
        ValidateNotes(notes);
        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new RecipeComponent(
            id ?? Guid.NewGuid(),
            parentRecipeId,
            ingredientId: ingredientId,
            subRecipeId: null,
            quantity: quantity,
            notes: string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim(),
            displayOrder: displayOrder,
            now: now);
    }

    public static RecipeComponent ForSubRecipe(
        Guid parentRecipeId,
        Guid subRecipeId,
        decimal quantity,
        string? notes,
        int displayOrder,
        TimeProvider? clock,
        Guid? id)
    {
        ValidateQuantity(quantity);
        ValidateNotes(notes);
        if (parentRecipeId == subRecipeId)
            throw new DomainException("A recipe component cannot reference its own recipe.");
        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new RecipeComponent(
            id ?? Guid.NewGuid(),
            parentRecipeId,
            ingredientId: null,
            subRecipeId: subRecipeId,
            quantity: quantity,
            notes: string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim(),
            displayOrder: displayOrder,
            now: now);
    }

    public void UpdateQuantity(decimal quantity)
    {
        ValidateQuantity(quantity);
        Quantity = quantity;
    }

    public void UpdateNotes(string? notes)
    {
        ValidateNotes(notes);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes!.Trim();
    }

    public void SetDisplayOrder(int order) => DisplayOrder = order;

    private static void ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Component quantity must be positive.");
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes is not null && notes.Length > 200)
            throw new DomainException("Component notes must be at most 200 characters.");
    }
}
