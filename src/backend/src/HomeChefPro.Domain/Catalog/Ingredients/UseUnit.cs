using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Ingredients;

public enum UseUnit
{
    [DbValue("g")]     Gram,
    [DbValue("ml")]    Milliliter,
    [DbValue("unit")]  Unit,
}
