using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using RpUtils.UI;

namespace RpUtils.Services;

/// <summary>
/// Sends formatted messages to the local player's game chat.
/// Messages are only visible to the local client — they are not sent to other players.
/// All messages are prefixed with a colored [RpUtils] tag.
/// </summary>
internal static class ChatEcho
{

    /// <summary>
    /// Prints a plain text message with the [RpUtils] prefix.
    /// </summary>
    public static void Send(string message)
    {
        var seString = new SeStringBuilder()
            .AddUiForeground(Theme.ChatPrefixColor)
            .AddText("[RpUtils] ")
            .AddUiForegroundOff()
            .AddText(message)
            .Build();

        Print(seString);
    }

    /// <summary>
    /// Prints a pre-built SeString body with the [RpUtils] prefix prepended.
    /// Use this for messages that need custom formatting (colored names, etc.).
    /// </summary>
    public static void Send(SeString body)
    {
        var prefix = new SeStringBuilder()
            .AddUiForeground(Theme.ChatPrefixColor)
            .AddText("[RpUtils] ")
            .AddUiForegroundOff()
            .Build();

        prefix.Append(body);
        Print(prefix);
    }

    private static void Print(SeString message)
    {
        Plugin.ChatGui.Print(new XivChatEntry
        {
            Message = message,
            Type = XivChatType.Echo,
        });
    }
}
