using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public enum OrderStatus
{
    [DbValue("pending_payment")]    PendingPayment,
    [DbValue("payment_verifying")]  PaymentVerifying,
    [DbValue("paid")]               Paid,
    [DbValue("in_preparation")]     InPreparation,
    [DbValue("ready")]              Ready,
    [DbValue("in_delivery")]        InDelivery,
    [DbValue("delivered")]          Delivered,
    [DbValue("cancelled")]          Cancelled,
    [DbValue("rejected")]           Rejected,
}
