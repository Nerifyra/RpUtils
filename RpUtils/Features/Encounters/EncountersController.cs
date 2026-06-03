using RpUtils.Features.Encounters.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.Features.Encounters;

public sealed class EncountersController : IEncountersController, IDisposable
{
    private readonly EncountersService _service;

    private readonly Dictionary<string, EncounterState> _encounters = [];

    public IReadOnlyDictionary<string, EncounterState> Encounters => _encounters;

    public event Action? OnStateChanged;

    public EncountersController(EncountersService service)
    {
        _service = service;

        _service.OnEncounterStateUpdated += OnEncounterStateUpdated;
        _service.OnEncounterEnded += OnEncounterEnded;
    }

    private void OnEncounterStateUpdated(EncounterState state)
    {
        _encounters.TryGetValue(state.EncounterId, out var previous);

        _encounters[state.EncounterId] = state;
        OnStateChanged?.Invoke();

        if (LocalPlayerTurnJustStarted(previous, state))
            EncounterAlerts.AlertYourTurn(state);
        else if (LocalPlayerOnDeckJustStarted(previous, state))
            EncounterAlerts.AlertOnDeck(state);
    }

    private static bool LocalPlayerTurnJustStarted(EncounterState? previous, EncounterState current)
    {
        // Skip the first update we see for an encounter — we can't distinguish "turn just advanced
        // to you" from "encounter was just created / you just joined and it happens to be your turn".
        // Real turn transitions always have a previous state to diff against.
        if (previous == null) return false;

        if (!Plugin.Lobbies.Lobbies.TryGetValue(current.LobbyId, out var lobby)) return false;

        var nowCurrent = current.Participants.Any(p => p.PlayerId == lobby.PlayerId && p.IsCurrent);
        if (!nowCurrent) return false;

        var wasCurrent = previous.Participants.Any(p => p.PlayerId == lobby.PlayerId && p.IsCurrent);
        return !wasCurrent;
    }

    private static bool LocalPlayerOnDeckJustStarted(EncounterState? previous, EncounterState current)
    {
        if (previous == null) return false;
        if (!Plugin.Lobbies.Lobbies.TryGetValue(current.LobbyId, out var lobby)) return false;

        var nowOnDeck = IsLocalPlayerOnDeck(current, lobby.PlayerId);
        if (!nowOnDeck) return false;

        var wasOnDeck = IsLocalPlayerOnDeck(previous, lobby.PlayerId);
        return !wasOnDeck;
    }

    private static bool IsLocalPlayerOnDeck(EncounterState state, string localPlayerId)
    {
        // Participants are stored in turn order, so "on deck" is the slot after the current one
        // (wrapping). Need at least two for "next" to mean something other than "current".
        if (state.Participants.Count < 2) return false;

        var currentIndex = state.Participants.FindIndex(p => p.IsCurrent);
        if (currentIndex < 0) return false;

        var nextIndex = (currentIndex + 1) % state.Participants.Count;
        return state.Participants[nextIndex].PlayerId == localPlayerId;
    }

    private void OnEncounterEnded(string encounterId)
    {
        _encounters.Remove(encounterId);
        OnStateChanged?.Invoke();
    }

    public async Task CreateEncounter(string lobbyId, string name, List<string> playerIds)
    {
        var success = await _service.UpdateEncounter(lobbyId, null, name, playerIds);
        if (!success)
        {
            Notify.Error("Failed to create encounter.");
        }
    }

    public async Task UpdateEncounter(string lobbyId, string encounterId, string name, List<string> playerIds)
    {
        var success = await _service.UpdateEncounter(lobbyId, encounterId, name, playerIds);
        if (!success)
        {
            Notify.Error("Failed to update encounter.");
        }
    }

    public async Task ReverseTurn(string encounterId)
    {
        var success = await _service.ReverseTurn(encounterId);
        if (!success)
        {
            Notify.Error("Failed to reverse turn.");
        }
    }

    public async Task AdvanceTurn(string encounterId)
    {
        var success = await _service.AdvanceTurn(encounterId);
        if (!success)
        {
            Notify.Error("Failed to advance turn.");
        }
    }

    public async Task SetInitiative(string encounterId, string participantId, int value)
    {
        var success = await _service.SetInitiative(encounterId, participantId, value);
        if (!success)
        {
            Notify.Error("Failed to set initiative.");
        }
    }

    public async Task UpsertNpc(string encounterId, UpsertNpcRequest request)
    {
        var success = await _service.UpsertNpc(encounterId, request);
        if (!success)
        {
            Notify.Error("Failed to update NPC.");
        }
    }

    public async Task RemoveNpcParticipant(string encounterId, string participantId)
    {
        var success = await _service.RemoveNpcParticipant(encounterId, participantId);
        if (!success)
        {
            Notify.Error("Failed to remove NPC.");
        }
    }

    public async Task EndEncounter(string encounterId)
    {
        var success = await _service.EndEncounter(encounterId);
        if (!success)
        {
            Notify.Error("Failed to end encounter.");
        }
    }

    public async Task RefreshEncounters(string lobbyId)
    {
        try
        {
            var encounters = await _service.GetLobbyEncounters(lobbyId);
            if (encounters is null) return;

            // Clear encounters for this lobby and repopulate
            foreach (var id in _encounters.Keys.Where(id => _encounters[id].LobbyId == lobbyId).ToList())
                _encounters.Remove(id);

            foreach (var encounter in encounters)
                _encounters[encounter.EncounterId] = encounter;

            OnStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to refresh encounters.");
        }
    }

    public void Dispose()
    {
        _service.OnEncounterStateUpdated -= OnEncounterStateUpdated;
        _service.OnEncounterEnded -= OnEncounterEnded;
    }
}
