using Dalamud.Interface.ImGuiNotification;

namespace RpUtils.Services;

internal static class Notify
{
    public static void Error(string message) =>
        Plugin.NotificationManager.AddNotification(new Notification
        {
            Content = message,
            Type = NotificationType.Error,
        });
}
