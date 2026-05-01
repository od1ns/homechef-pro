using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public enum CustomerType
{
    [DbValue("registered")] Registered,
    [DbValue("guest")]      Guest,
}
