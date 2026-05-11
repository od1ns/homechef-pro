using FirebaseAdmin.Messaging;
using HomeChefPro.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeChefPro.Infrastructure.Notifications;

/// <summary>
/// Etapa 5: envía notificaciones push via Firebase Cloud Messaging (FCM v1).
/// Se registra en DI solo cuando la variable Firebase:ServiceAccountPath apunta a un archivo válido.
/// </summary>
internal sealed class FcmNotificationService(
    IServiceScopeFactory scopeFactory,
    ILogger<FcmNotificationService> logger) : INotificationService
{
    public async Task NotifyOrderAsync(Guid orderId, string title, string body, CancellationToken ct = default)
    {
        try
        {
            // IHomeChefProDbContext es scoped → necesitamos un scope propio
            // (FcmNotificationService es singleton).
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IHomeChefProDbContext>();

            var tokenRow = await db.OrderDeviceTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.OrderId == orderId, ct)
                .ConfigureAwait(false);

            if (tokenRow is null)
            {
                logger.LogDebug("No FCM token for order {OrderId} — skipping notification.", orderId);
                return;
            }

            var message = new Message
            {
                Token = tokenRow.FcmToken,
                Notification = new Notification { Title = title, Body = body },
                Android = new AndroidConfig
                {
                    Priority = Priority.High,
                    Notification = new AndroidNotification
                    {
                        Sound = "default",
                        ClickAction = "FLUTTER_NOTIFICATION_CLICK",
                    },
                },
                Data = new Dictionary<string, string>
                {
                    ["orderId"] = orderId.ToString(),
                },
            };

            var messageId = await FirebaseMessaging.DefaultInstance
                .SendAsync(message, ct)
                .ConfigureAwait(false);

            logger.LogInformation(
                "FCM sent for order {OrderId}: messageId={MessageId}", orderId, messageId);
        }
        catch (Exception ex)
        {
            // Las notificaciones son best-effort: nunca deben romper el flujo principal.
            logger.LogWarning(ex, "FCM send failed for order {OrderId}.", orderId);
        }
    }
}
