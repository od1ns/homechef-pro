using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Inventory;

public enum WasteReason
{
    [DbValue("spoiled")]     Spoiled,
    [DbValue("burnt")]       Burnt,
    [DbValue("dropped")]     Dropped,
    [DbValue("expired")]     Expired,
    [DbValue("over_prepped")] OverPrepped,
    [DbValue("theft")]       Theft,
    [DbValue("other")]       Other,
}
