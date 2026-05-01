using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes;

public enum YieldUnit
{
    [DbValue("g")]       Gram,
    [DbValue("ml")]      Milliliter,
    [DbValue("portion")] Portion,
    [DbValue("unit")]    Unit,
}
