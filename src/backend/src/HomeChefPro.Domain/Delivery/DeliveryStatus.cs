using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Delivery;

public enum DeliveryStatus
{
    [DbValue("assigned")]   Assigned,
    [DbValue("picked_up")]  PickedUp,
    [DbValue("on_the_way")] OnTheWay,
    [DbValue("delivered")]  Delivered,
    [DbValue("failed")]     Failed,
    [DbValue("cancelled")]  Cancelled,
    [DbValue("unknown")]    Unknown,
}
