namespace HomeChefPro.Domain.Orders;

/// <summary>
/// Etapa 5: token FCM del dispositivo del cliente asociado a un pedido.
/// Permite enviar notificaciones push cuando el estado del pedido cambia.
/// </summary>
public sealed class OrderDeviceToken
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string FcmToken { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private OrderDeviceToken() { }

    public static OrderDeviceToken Create(Guid orderId, string fcmToken, TimeProvider? clock = null)
    {
        if (orderId == Guid.Empty) throw new ArgumentException("OrderId is required.", nameof(orderId));
        if (string.IsNullOrWhiteSpace(fcmToken)) throw new ArgumentException("FcmToken is required.", nameof(fcmToken));

        return new OrderDeviceToken
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            FcmToken = fcmToken.Trim(),
            CreatedAt = (clock ?? TimeProvider.System).GetUtcNow(),
        };
    }
}
