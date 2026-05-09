using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes;

/// <summary>
/// Etapa 2: opcion de personalizacion del chef para un plato.
/// Ejemplos: "Sin cebolla" (precio 0), "Extra queso" (+$0.50), "Aguacate" (+$1.00).
/// </summary>
public sealed class RecipeModifier : Entity<Guid>
{
    public Guid ChefId { get; private set; }
    public Guid RecipeId { get; private set; }

    public string Name { get; private set; } = null!;
    public int DefaultQty { get; private set; }
    public int MinQty { get; private set; }
    public int MaxQty { get; private set; }
    public decimal PriceDeltaUsd { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private RecipeModifier() { }

    internal static RecipeModifier Create(
        Guid recipeId,
        string name,
        int defaultQty,
        int minQty,
        int maxQty,
        decimal priceDeltaUsd,
        int displayOrder,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        ValidateParams(name, defaultQty, minQty, maxQty);
        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new RecipeModifier
        {
            Id = id ?? Guid.NewGuid(),
            RecipeId = recipeId,
            Name = name.Trim(),
            DefaultQty = defaultQty,
            MinQty = minQty,
            MaxQty = maxQty,
            PriceDeltaUsd = priceDeltaUsd,
            DisplayOrder = displayOrder,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    internal void Update(
        string name,
        int defaultQty,
        int minQty,
        int maxQty,
        decimal priceDeltaUsd,
        int displayOrder,
        TimeProvider? clock = null)
    {
        ValidateParams(name, defaultQty, minQty, maxQty);
        Name = name.Trim();
        DefaultQty = defaultQty;
        MinQty = minQty;
        MaxQty = maxQty;
        PriceDeltaUsd = priceDeltaUsd;
        DisplayOrder = displayOrder;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    internal void Deactivate(TimeProvider? clock = null)
    {
        IsActive = false;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    private static void ValidateParams(string name, int defaultQty, int minQty, int maxQty)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre del modificador es requerido.");
        if (name.Length > 200)
            throw new DomainException("El nombre del modificador no puede superar 200 caracteres.");
        if (minQty < 0)
            throw new DomainException("min_qty no puede ser negativo.");
        if (maxQty < minQty)
            throw new DomainException("max_qty debe ser >= min_qty.");
        if (defaultQty < minQty || defaultQty > maxQty)
            throw new DomainException("default_qty debe estar en el rango [min_qty, max_qty].");
    }
}
