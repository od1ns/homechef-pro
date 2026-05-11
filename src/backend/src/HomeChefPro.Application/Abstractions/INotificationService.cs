namespace HomeChefPro.Application.Abstractions;

/// <summary>
/// Etapa 5: envío de notificaciones push al cliente.
/// Implementación real en Infrastructure via FCM; NullNotificationService para dev sin Firebase.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Envía una notificación push al dispositivo registrado para el pedido indicado.
    /// No lanza excepción si no hay token registrado o si FCM no está configurado.
    /// </summary>
    Task NotifyOrderAsync(Guid orderId, string title, string body, CancellationToken ct = default);
}
