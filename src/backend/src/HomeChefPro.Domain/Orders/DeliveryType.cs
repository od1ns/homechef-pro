using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public enum DeliveryType
{
    [DbValue("pickup")]      Pickup,
    [DbValue("third_party")] ThirdParty,
}
