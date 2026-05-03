using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes;

public sealed class Recipe : AggregateRoot<Guid>
{
    private readonly List<RecipeComponent> _components = [];

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public bool IsSubRecipe { get; private set; }

    public string? ProcedureMarkdown { get; private set; }

    public decimal? YieldQuantity { get; private set; }
    public YieldUnit? YieldUnit { get; private set; }

    public decimal? SuggestedPriceUsd { get; private set; }
    public decimal? SellingPriceUsd { get; private set; }

    public int PrepTimeMinutes { get; private set; }
    public string? ImageUrl { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsOutOfStock { get; private set; }

    public MenuType MenuType { get; private set; }
    public DateTimeOffset? SpecialFrom { get; private set; }
    public DateTimeOffset? SpecialTo { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<RecipeComponent> Components => _components.AsReadOnly();

    private Recipe() { }

    private Recipe(
        Guid id,
        string name,
        string? description,
        string? category,
        bool isSubRecipe,
        decimal? yieldQuantity,
        YieldUnit? yieldUnit,
        decimal? sellingPriceUsd,
        int prepTimeMinutes,
        MenuType menuType,
        DateTimeOffset? specialFrom,
        DateTimeOffset? specialTo,
        DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Description = description;
        Category = category;
        IsSubRecipe = isSubRecipe;
        YieldQuantity = yieldQuantity;
        YieldUnit = yieldUnit;
        SellingPriceUsd = sellingPriceUsd;
        PrepTimeMinutes = prepTimeMinutes;
        MenuType = menuType;
        SpecialFrom = specialFrom;
        SpecialTo = specialTo;
        IsActive = true;
        IsOutOfStock = false;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Recipe CreateDish(
        string name,
        decimal sellingPriceUsd,
        int prepTimeMinutes = 0,
        MenuType menuType = MenuType.Fixed,
        DateTimeOffset? specialFrom = null,
        DateTimeOffset? specialTo = null,
        string? description = null,
        string? category = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        ValidateName(name);
        if (sellingPriceUsd <= 0)
            throw new DomainException("Selling price must be positive for a dish.");
        if (prepTimeMinutes < 0)
            throw new DomainException("Prep time cannot be negative.");
        ValidateSpecialWindow(menuType, specialFrom, specialTo);

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Recipe(
            id ?? Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            isSubRecipe: false,
            yieldQuantity: null,
            yieldUnit: null,
            sellingPriceUsd: sellingPriceUsd,
            prepTimeMinutes: prepTimeMinutes,
            menuType: menuType,
            specialFrom: specialFrom,
            specialTo: specialTo,
            now: now);
    }

    public static Recipe CreateSubRecipe(
        string name,
        decimal yieldQuantity,
        YieldUnit yieldUnit,
        int prepTimeMinutes = 0,
        string? description = null,
        string? category = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        ValidateName(name);
        if (yieldQuantity <= 0)
            throw new DomainException("Sub-recipe yield quantity must be positive.");
        if (prepTimeMinutes < 0)
            throw new DomainException("Prep time cannot be negative.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Recipe(
            id ?? Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            isSubRecipe: true,
            yieldQuantity: yieldQuantity,
            yieldUnit: yieldUnit,
            sellingPriceUsd: null,
            prepTimeMinutes: prepTimeMinutes,
            menuType: MenuType.Fixed,
            specialFrom: null,
            specialTo: null,
            now: now);
    }

    public RecipeComponent AddIngredient(
        Guid ingredientId,
        decimal quantity,
        string? notes = null,
        int displayOrder = 0,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (_components.Any(c => c.IngredientId == ingredientId))
            throw new DomainException(
                $"Recipe '{Name}' already contains ingredient {ingredientId}.");

        var component = RecipeComponent.ForIngredient(
            parentRecipeId: Id,
            ingredientId: ingredientId,
            quantity: quantity,
            notes: notes,
            displayOrder: displayOrder,
            clock: clock,
            id: id);
        _components.Add(component);
        // No Touch: trigger SQL maneja updated_at.
        return component;
    }

    public RecipeComponent AddSubRecipe(
        Guid subRecipeId,
        decimal quantity,
        string? notes = null,
        int displayOrder = 0,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (subRecipeId == Id)
            throw new DomainException("A recipe cannot contain itself.");
        if (_components.Any(c => c.SubRecipeId == subRecipeId))
            throw new DomainException(
                $"Recipe '{Name}' already contains sub-recipe {subRecipeId}.");

        var component = RecipeComponent.ForSubRecipe(
            parentRecipeId: Id,
            subRecipeId: subRecipeId,
            quantity: quantity,
            notes: notes,
            displayOrder: displayOrder,
            clock: clock,
            id: id);
        _components.Add(component);
        // No Touch: trigger SQL maneja updated_at.
        return component;
    }

    public void RemoveComponent(Guid componentId, TimeProvider? clock = null)
    {
        var component = _components.FirstOrDefault(c => c.Id == componentId)
            ?? throw new DomainException($"Component {componentId} not found in recipe '{Name}'.");
        _components.Remove(component);
        Touch(clock);
    }

    public void UpdateSellingPrice(decimal sellingPriceUsd, TimeProvider? clock = null)
    {
        if (IsSubRecipe)
            throw new DomainException("Sub-recipes do not have a selling price.");
        if (sellingPriceUsd <= 0)
            throw new DomainException("Selling price must be positive.");
        SellingPriceUsd = sellingPriceUsd;
        Touch(clock);
    }

    public void RecordSuggestedPrice(decimal suggestedPriceUsd, TimeProvider? clock = null)
    {
        if (suggestedPriceUsd < 0)
            throw new DomainException("Suggested price cannot be negative.");
        SuggestedPriceUsd = suggestedPriceUsd;
        Touch(clock);
    }

    public void UpdateYield(decimal yieldQuantity, YieldUnit yieldUnit, TimeProvider? clock = null)
    {
        if (!IsSubRecipe)
            throw new DomainException("Dishes (non-sub-recipes) cannot have a yield.");
        if (yieldQuantity <= 0)
            throw new DomainException("Yield quantity must be positive.");
        YieldQuantity = yieldQuantity;
        YieldUnit = yieldUnit;
        Touch(clock);
    }

    public void UpdateProcedure(string? markdown, TimeProvider? clock = null)
    {
        ProcedureMarkdown = string.IsNullOrWhiteSpace(markdown) ? null : markdown;
        Touch(clock);
    }

    public void UpdatePrepTime(int minutes, TimeProvider? clock = null)
    {
        if (minutes < 0)
            throw new DomainException("Prep time cannot be negative.");
        PrepTimeMinutes = minutes;
        Touch(clock);
    }

    public void UpdateImage(string? imageUrl, TimeProvider? clock = null)
    {
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        Touch(clock);
    }

    public void MarkOutOfStock(TimeProvider? clock = null)
    {
        IsOutOfStock = true;
        Touch(clock);
    }

    public void MarkBackInStock(TimeProvider? clock = null)
    {
        IsOutOfStock = false;
        Touch(clock);
    }

    public void PromoteToDailySpecial(DateTimeOffset from, DateTimeOffset to, TimeProvider? clock = null)
    {
        if (IsSubRecipe)
            throw new DomainException("Sub-recipes cannot be promoted to daily special.");
        if (from >= to)
            throw new DomainException("Daily special window: 'from' must be before 'to'.");
        MenuType = MenuType.DailySpecial;
        SpecialFrom = from;
        SpecialTo = to;
        Touch(clock);
    }

    public void DemoteToFixedMenu(TimeProvider? clock = null)
    {
        MenuType = MenuType.Fixed;
        SpecialFrom = null;
        SpecialTo = null;
        Touch(clock);
    }

    public void Deactivate(TimeProvider? clock = null)
    {
        IsActive = false;
        Touch(clock);
    }

    public void Activate(TimeProvider? clock = null)
    {
        IsActive = true;
        Touch(clock);
    }

    public bool IsOnMenuAt(DateTimeOffset at) =>
        IsActive
        && !IsOutOfStock
        && (MenuType == MenuType.Fixed
            || (SpecialFrom is { } f && SpecialTo is { } t && at >= f && at < t));

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Recipe name is required.");
        if (name.Length > 200)
            throw new DomainException("Recipe name must be at most 200 characters.");
    }

    private static void ValidateSpecialWindow(MenuType menuType, DateTimeOffset? from, DateTimeOffset? to)
    {
        if (menuType != MenuType.DailySpecial) return;
        if (from is null || to is null)
            throw new DomainException("Daily specials require both 'from' and 'to'.");
        if (from >= to)
            throw new DomainException("Daily special window: 'from' must be before 'to'.");
    }

    private void Touch(TimeProvider? clock) =>
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
}
