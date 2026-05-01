using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Payments;

public enum PaymentMethod
{
    [DbValue("pago_movil")]   PagoMovil,
    [DbValue("transfer_ves")] TransferVes,
    [DbValue("transfer_usd")] TransferUsd,
    [DbValue("zelle")]        Zelle,
    [DbValue("binance_pay")]  BinancePay,
    [DbValue("cash")]         Cash,
}
