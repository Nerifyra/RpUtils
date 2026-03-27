using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using RpUtils.Features.Encounters.Models;
using RpUtils.Features.Rolls.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RpUtils.Features.Rolls;

/// <summary>
/// Listens for dice roll messages in chat and automatically submits results
/// to active roll requests created by the local player.
///
/// Supported chat formats:
///   /random in say chat (own):   "Random! You roll a 511."
///   /random in say chat (other): "Random! Firstname Lastname rolls a 31."
///   /dice in FC/party/other:     "Random! 657" (sender SeString has the player name)
///
/// Character names are extracted from SeString PlayerPayloads for reliable matching
/// against stored "Name@World" format, avoiding issues with decorated names or
/// invisible control characters in chat text.
/// </summary>
public sealed class ChatRollListener : IDisposable
{
    private static readonly Regex OwnRollRegex = new(@"Random! You roll a (\d+)\.", RegexOptions.Compiled);
    private static readonly Regex OtherSayRollRegex = new(@"Random! .+ rolls a (\d+)\.", RegexOptions.Compiled);
    private static readonly Regex DiceRollRegex = new(@"^Random! (\d+)$", RegexOptions.Compiled);

    public ChatRollListener()
    {
        Plugin.ChatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        Plugin.ChatGui.ChatMessage -= OnChatMessage;
    }

    // ── Chat event handler (runs on main thread) ──────────────────────────

    private void OnChatMessage(XivChatType type, int senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            var parsed = TryParseRoll(sender, message);
            if (parsed == null) return;

            var (rollValue, rollerName, isLocalPlayer) = parsed.Value;

            Plugin.Log.Info("Chat roll detected: '{RollerName}' rolled {Value} (isLocal={IsLocal})",
                rollerName, rollValue, isLocalPlayer);

            var myPlayerId = Plugin.Lobbies.Lobbies.Values
                .Select(l => l.PlayerId)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(myPlayerId)) return;

            Task.Run(() => ProcessRollAsync(rollerName, rollValue, myPlayerId, isLocalPlayer));
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error handling chat roll message.");
        }
    }

    // ── Message parsing ───────────────────────────────────────────────────

    /// <summary>
    /// Attempts to parse a roll result from a chat message.
    /// Returns the roll value, the roller's "Name@World" identifier, and whether it's the local player.
    /// Must run on the main thread (accesses ObjectTable).
    /// </summary>
    private static (int Value, string CharacterName, bool IsLocalPlayer)? TryParseRoll(SeString sender, SeString message)
    {
        var messageText = message.TextValue;

        // Try each format in order
        var rollValue = TryMatchOwnRoll(messageText)
            ?? TryMatchOtherSayRoll(messageText)
            ?? TryMatchDiceRoll(messageText);

        if (rollValue == null) return null;

        // Build local player's "Name@World" identifier (must happen on main thread)
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer == null) return null;

        var localName = localPlayer.Name.TextValue;
        var localWorld = Plugin.PlayerState.CurrentWorld.Value.Name.ToString();
        var localCharacterName = $"{localName}@{localWorld}";

        // Determine who rolled — own rolls use local player, others use PlayerPayload
        string rollerName;
        if (OwnRollRegex.IsMatch(messageText))
        {
            rollerName = localCharacterName;
        }
        else
        {
            rollerName = ExtractCharacterName(message) ?? ExtractCharacterName(sender) ?? localCharacterName;
        }

        var isLocal = string.Equals(rollerName, localCharacterName, StringComparison.OrdinalIgnoreCase);
        return (rollValue.Value, rollerName, isLocal);
    }

    private static int? TryMatchOwnRoll(string text)
    {
        var match = OwnRollRegex.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static int? TryMatchOtherSayRoll(string text)
    {
        var match = OtherSayRollRegex.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    private static int? TryMatchDiceRoll(string text)
    {
        var match = DiceRollRegex.Match(text);
        return match.Success ? int.Parse(match.Groups[1].Value) : null;
    }

    /// <summary>
    /// Extracts a "Name@World" identifier from a SeString's PlayerPayload.
    /// </summary>
    private static string? ExtractCharacterName(SeString seString)
    {
        var payload = seString.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (payload == null) return null;

        var name = payload.PlayerName;
        if (string.IsNullOrEmpty(name)) return null;

        var world = payload.World.Value.Name.ToString();
        return string.IsNullOrEmpty(world) ? name : $"{name}@{world}";
    }

    // ── Roll assignment (runs off main thread) ────────────────────────────

    private async Task ProcessRollAsync(string characterName, int value, string myPlayerId, bool isLocalPlayer)
    {
        try
        {
            var myActiveRolls = Plugin.Rolls.RollRequests.Values
                .Where(r => r.IsActive && r.CreatedByPlayerId == myPlayerId)
                .OrderBy(r => r.CreatedAtUtc)
                .ToList();

            foreach (var roll in myActiveRolls)
            {
                var encounter = Plugin.Encounters.Encounters.Values
                    .FirstOrDefault(e => e.EncounterId == roll.EncounterId);

                if (encounter == null) continue;

                // Try to match a pending player participant by "Name@World"
                var participant = FindPendingPlayer(roll, characterName);
                if (participant != null)
                {
                    await SubmitRollResult(roll, encounter, participant, value);
                    return;
                }

                // If the DM rolled and no player matched, assign to the next pending NPC
                if (isLocalPlayer)
                {
                    var npc = FindNextPendingNpc(roll, encounter);
                    if (npc != null)
                    {
                        await SubmitRollResult(roll, encounter, npc, value);
                        return;
                    }
                }
            }

            Plugin.Log.Debug("No pending participant found for roll by '{CharacterName}'", characterName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Error processing roll assignment.");
        }
    }

    // ── Participant matching ──────────────────────────────────────────────

    private static RollParticipant? FindPendingPlayer(RollRequestState roll, string characterName)
    {
        return roll.Participants.FirstOrDefault(p =>
            p.IsPending && !p.IsNpc &&
            string.Equals(p.CharacterName, characterName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds the next pending NPC in encounter initiative order (top of the list first).
    /// </summary>
    private static RollParticipant? FindNextPendingNpc(RollRequestState roll, EncounterState encounter)
    {
        var pendingNpcIds = roll.Participants
            .Where(p => p.IsPending && p.IsNpc)
            .Select(p => p.ParticipantId)
            .ToHashSet();

        if (pendingNpcIds.Count == 0) return null;

        // Walk the encounter participant list (sorted by initiative) to find the first pending NPC
        var firstId = encounter.Participants
            .Where(p => pendingNpcIds.Contains(p.ParticipantId))
            .Select(p => p.ParticipantId)
            .FirstOrDefault();

        return firstId != null
            ? roll.Participants.First(p => p.ParticipantId == firstId)
            : roll.Participants.FirstOrDefault(p => p.IsPending && p.IsNpc);
    }

    // ── Submission ────────────────────────────────────────────────────────

    private static async Task SubmitRollResult(RollRequestState roll, EncounterState encounter, RollParticipant participant, int value)
    {
        var label = participant.IsNpc ? "NPC" : "participant";
        Plugin.Log.Info("Submitting roll {Value} for {Label} '{DisplayName}' in roll '{RollName}'",
            value, label, participant.DisplayName, roll.Name);

        await Plugin.Rolls.SubmitRoll(roll.RollRequestId, participant.ParticipantId, value);

        if (roll.IsInitiativeRoll)
        {
            await Plugin.Encounters.SetInitiative(encounter.EncounterId, participant.ParticipantId, value);
        }
    }
}
