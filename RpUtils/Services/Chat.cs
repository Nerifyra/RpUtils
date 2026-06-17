using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using RpUtils.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpUtils.Services;

public enum InputChannel : uint
{
    Tell = 0,
    Say = 1,
    Party = 2,
    Alliance = 3,
    Yell = 4,
    Shout = 5,
    FreeCompany = 6,
    PvpTeam = 7,
    NoviceNetwork = 8,
    CrossLinkshell1 = 9,
    CrossLinkshell2 = 10,
    CrossLinkshell3 = 11,
    CrossLinkshell4 = 12,
    CrossLinkshell5 = 13,
    CrossLinkshell6 = 14,
    CrossLinkshell7 = 15,
    CrossLinkshell8 = 16,
    Linkshell1 = 19,
    Linkshell2 = 20,
    Linkshell3 = 21,
    Linkshell4 = 22,
    Linkshell5 = 23,
    Linkshell6 = 24,
    Linkshell7 = 25,
    Linkshell8 = 26,
}

/// <summary>
///   • <see cref="Echo"/>        — prints locally only; never leaves the client.
///   • <see cref="SendCommand"/> — submits a trusted, fixed command we author (e.g. /roleplaying).
///   • <see cref="SendMessage"/> — posts player-visible text to the current channel, sanitized
///                                 and restricted to safe channels (shout/yell/etc. are refused).
/// </summary>
internal static class Chat
{
    private const int MaxMessageBytes = 500;

    private static readonly HashSet<InputChannel> Broadcastable =
    [
        InputChannel.Say, InputChannel.Party, InputChannel.Alliance, InputChannel.FreeCompany, InputChannel.Tell,
        InputChannel.Linkshell1, InputChannel.Linkshell2, InputChannel.Linkshell3, InputChannel.Linkshell4,
        InputChannel.Linkshell5, InputChannel.Linkshell6, InputChannel.Linkshell7, InputChannel.Linkshell8,
        InputChannel.CrossLinkshell1, InputChannel.CrossLinkshell2, InputChannel.CrossLinkshell3, InputChannel.CrossLinkshell4,
        InputChannel.CrossLinkshell5, InputChannel.CrossLinkshell6, InputChannel.CrossLinkshell7, InputChannel.CrossLinkshell8,
    ];

    /// <summary>Prints a plain text message to the local chat log with the [RpUtils] prefix.</summary>
    public static void Echo(string message)
    {
        var seString = new SeStringBuilder()
            .AddUiForeground(Theme.ChatPrefixColor)
            .AddText("[RpUtils] ")
            .AddUiForegroundOff()
            .AddText(message)
            .Build();

        Print(seString);
    }

    /// <summary>Prints a pre-built SeString body to the local chat log with the [RpUtils] prefix.</summary>
    public static void Echo(SeString body)
    {
        var prefixed = new SeStringBuilder()
            .AddUiForeground(Theme.ChatPrefixColor)
            .AddText("[RpUtils] ")
            .AddUiForegroundOff()
            .Build();

        prefixed.Append(body);
        Print(prefixed);
    }

    public static void SendCommand(string command) => Submit(command);

    /// <summary>
    /// Posts <paramref name="text"/> to the currently selected channel, but only if that channel
    /// is a safe broadcast target.
    /// </summary>
    public static bool SendMessage(string text)
    {
        if (!Broadcastable.Contains(CurrentChannel()))
            return false;

        var message = Sanitize(text);
        if (message.Length == 0)
            return false;

        Submit(message);
        return true;
    }

    public static unsafe InputChannel CurrentChannel() =>
        (InputChannel)(uint)RaptureShellModule.Instance()->ChatType;

    public static bool CurrentChannelCanMessage() => Broadcastable.Contains(CurrentChannel());

    private static unsafe void Submit(string line)
    {
        var entry = Utf8String.FromString(line);
        try
        {
            UIModule.Instance()->ProcessChatBoxEntry(entry);
        }
        finally
        {
            entry->Dtor(true);
        }
    }

    private static void Print(SeString message)
    {
        Plugin.ChatGui.Print(new XivChatEntry
        {
            Message = message,
            Type = XivChatType.Echo,
        });
    }

    private static string Sanitize(string text)
    {
        var oneLine = text.Replace('\r', ' ').Replace('\n', ' ').Trim();

        if (Encoding.UTF8.GetByteCount(oneLine) <= MaxMessageBytes)
            return oneLine;

        var span = oneLine.AsSpan();
        while (span.Length > 0 && Encoding.UTF8.GetByteCount(span) > MaxMessageBytes)
            span = span[..^1];
        return span.ToString();
    }
}

