using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Inventory;

public enum MovementType
{
    [DbValue("purchase")]   Purchase,
    [DbValue("waste")]      Waste,
    [DbValue("sale")]       Sale,
    [DbValue("adjustment")] Adjustment,
    [DbValue("initial")]    Initial,
}
