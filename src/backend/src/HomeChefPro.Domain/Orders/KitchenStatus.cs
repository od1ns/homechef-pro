using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public enum KitchenStatus
{
    [DbValue("pending")] Pending,
    [DbValue("in_prep")] InPrep,
    [DbValue("ready")]   Ready,
}
