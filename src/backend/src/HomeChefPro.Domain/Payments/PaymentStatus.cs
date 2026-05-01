using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Payments;

public enum PaymentStatus
{
    [DbValue("pending")]  Pending,
    [DbValue("verified")] Verified,
    [DbValue("rejected")] Rejected,
}
