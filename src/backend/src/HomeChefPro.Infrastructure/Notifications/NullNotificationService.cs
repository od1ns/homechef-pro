using HomeChefPro.Application.Abstractions;

namespace HomeChefPro.Infrastructure.Notifications;

/// <summary>
/// Etapa 5: implementación vacía para entornos sin Firebase configurado (dev local, CI).
/// </summary>
internal sealed class NullNotificationService : INotificationService
{
    public Task NotifyOrderAsync(Guid orderId, string title, string body, CancellationToken ct = default)
        => Task.CompletedTask;
}
