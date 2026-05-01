using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Ingredients;

public enum PurchaseUnit
{
    [DbValue("kg")]     Kilogram,
    [DbValue("g")]      Gram,
    [DbValue("l")]      Liter,
    [DbValue("ml")]     Milliliter,
    [DbValue("unit")]   Unit,
    [DbValue("box")]    Box,
    [DbValue("sack")]   Sack,
    [DbValue("bag")]    Bag,
    [DbValue("bottle")] Bottle,
    [DbValue("pack")]   Pack,
}
