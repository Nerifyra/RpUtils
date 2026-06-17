using FFXIVClientStructs.FFXIV.Client.UI;
using RpUtils.Features.Encounters.Models;
using RpUtils.Services;

namespace RpUtils.Features.Encounters;

internal static class EncounterAlerts
{
    // ── Your turn ─────────────────────────────────────────────────────

    public static void AlertYourTurn(EncounterState state)
    {
        var config = Plugin.Configuration;
        if (config.YourTurnChatAlert) EchoYourTurn(state);
        if (config.YourTurnSoundAlert) PlaySoundYourTurn();
    }

    private static void EchoYourTurn(EncounterState state)
    {
        var encounterName = string.IsNullOrWhiteSpace(state.Name) ? "the encounter" : state.Name;
        Chat.Echo($"It's your turn in {encounterName}!");
    }

    private static void PlaySoundYourTurn()
    {
        UIGlobals.PlayChatSoundEffect(Plugin.Configuration.YourTurnSoundId);
    }

    // ── On deck (you're next) ─────────────────────────────────────────

    public static void AlertOnDeck(EncounterState state)
    {
        var config = Plugin.Configuration;
        if (config.OnDeckChatAlert) EchoOnDeck(state);
        if (config.OnDeckSoundAlert) PlaySoundOnDeck();
    }

    private static void EchoOnDeck(EncounterState state)
    {
        var encounterName = string.IsNullOrWhiteSpace(state.Name) ? "the encounter" : state.Name;
        Chat.Echo($"You're next in {encounterName}.");
    }

    private static void PlaySoundOnDeck()
    {
        UIGlobals.PlayChatSoundEffect(Plugin.Configuration.OnDeckSoundId);
    }
}
