using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.UI;
using RpUtils.Features.Rolls.Models;
using RpUtils.Services;
using RpUtils.UI;
using System.Collections.Generic;
using System.Linq;

namespace RpUtils.Features.Rolls;

internal static class RollAlerts
{
    // ── Roll requested ────────────────────────────────────────────────

    public static void AlertRollRequested(RollRequestState state)
    {
        // Only alert players being asked to roll — other lobby members (DM, spectators) don't need it.
        if (!IsLocalPlayerParticipant(state)) return;

        var config = Plugin.Configuration;
        if (config.RollRequestedChatAlert) EchoRollRequested(state);
        if (config.RollRequestedSoundAlert) PlaySoundRollRequested();
    }

    private static void EchoRollRequested(RollRequestState state)
    {
        var dmName = ResolveDmDisplayName(state);

        if (state.IsInitiativeRoll)
        {
            ChatEcho.Send($"Roll for initiative! Please roll in a channel visible to {dmName}.");
            return;
        }

        var participantNames = string.Join(", ", state.Participants.Select(p => p.DisplayName));
        var dcText = state.DC.HasValue ? $"DC {state.DC.Value} " : "";
        ChatEcho.Send($"{dmName} has requested a {dcText}roll from {participantNames}. Please roll in a channel visible to {dmName}.");
    }

    private static void PlaySoundRollRequested()
    {
        UIGlobals.PlayChatSoundEffect(Plugin.Configuration.RollRequestedSoundId);
    }

    // ── Roll completed ────────────────────────────────────────────────

    public static void AlertRollCompleted(RollRequestState state)
    {
        var resolved = state.Participants.Where(p => !p.IsPending).ToList();
        if (resolved.Count == 0) return;

        var config = Plugin.Configuration;
        if (config.RollResultsChatAlert) EchoRollCompleted(state, resolved);
        if (config.RollResultsSoundAlert) PlaySoundRollCompleted();
    }

    private static void EchoRollCompleted(RollRequestState state, List<RollParticipant> resolved)
    {
        if (state.IsInitiativeRoll)
        {
            EchoInitiativeCompleted(resolved);
            return;
        }

        var rollLabel = string.IsNullOrWhiteSpace(state.Name) ? "Roll" : state.Name;

        if (state.DC.HasValue)
            EchoDcRollCompleted(rollLabel, state.DC.Value, resolved);
        else
            ChatEcho.Send($"{rollLabel} completed. {FormatResults(resolved)}");
    }

    private static void PlaySoundRollCompleted()
    {
        UIGlobals.PlayChatSoundEffect(Plugin.Configuration.RollResultsSoundId);
    }

    // ── Echo formatting ───────────────────────────────────────────────

    private static void EchoDcRollCompleted(string rollLabel, int dc, List<RollParticipant> resolved)
    {
        var passed = resolved.Where(p => p.Result!.Value >= dc).ToList();
        var failed = resolved.Where(p => p.Result!.Value < dc).ToList();

        var body = new SeStringBuilder();
        body.AddText($"DC {dc} {rollLabel} completed. ");

        if (passed.Count > 0)
        {
            AppendColoredResults(body, Theme.ChatGreenColor, passed);
            body.AddText(" succeeded. ");
        }

        if (failed.Count > 0)
        {
            AppendColoredResults(body, Theme.ChatRedColor, failed);
            body.AddText(" failed.");
        }

        ChatEcho.Send(body.Build());
    }

    private static void EchoInitiativeCompleted(List<RollParticipant> resolved)
    {
        var turnOrder = resolved.OrderByDescending(p => p.Result!.Value).ToList();

        var body = new SeStringBuilder();
        body.AddText("Initiative rolled. Turn order: ");

        for (var i = 0; i < turnOrder.Count; i++)
        {
            if (i > 0)
                body.AddText(", ");

            body.AddUiForeground(Theme.ChatHighlightColor);
            body.AddText(turnOrder[i].DisplayName);
            body.AddUiForegroundOff();
            body.AddText($" ({turnOrder[i].Result})");
        }

        ChatEcho.Send(body.Build());
    }

    private static void AppendColoredResults(SeStringBuilder body, ushort color, List<RollParticipant> participants)
    {
        body.AddUiForeground(color);
        body.AddText(string.Join(", ", participants.Select(p => $"{p.DisplayName} ({p.Result})")));
        body.AddUiForegroundOff();
    }

    private static string FormatResults(List<RollParticipant> resolved)
    {
        return string.Join(", ", resolved.Select(p => $"{p.DisplayName}: {p.Result}"));
    }

    // ── Lobby lookups ─────────────────────────────────────────────────

    private static bool IsLocalPlayerParticipant(RollRequestState state)
    {
        return FindLocalLobby(state.LobbyId) is { } lobby
            && state.Participants.Any(p => p.PlayerId == lobby.PlayerId);
    }

    private static string ResolveDmDisplayName(RollRequestState state)
    {
        return FindLocalLobby(state.LobbyId)
            ?.State.Members.FirstOrDefault(m => m.PlayerId == state.CreatedByPlayerId)
            ?.DisplayName ?? "DM";
    }

    private static Features.Lobbies.Models.Lobby? FindLocalLobby(string lobbyId)
    {
        return Plugin.Lobbies.Lobbies.Values.FirstOrDefault(l => l.LobbyId == lobbyId);
    }
}
