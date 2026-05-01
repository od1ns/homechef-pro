using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Payments;

public enum PaidCurrency
{
    [DbValue("USD")] Usd,
    [DbValue("VES")] Ves,
}
