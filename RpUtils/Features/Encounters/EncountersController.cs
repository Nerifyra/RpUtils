using Dalamud.Interface.ImGuiNotification;
using RpUtils.Features.Encounters.Models;
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
        _encounters[state.EncounterId] = state;
        OnStateChanged?.Invoke();
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
            ShowError("Failed to create encounter.");
        }
    }

    public async Task UpdateEncounter(string lobbyId, string encounterId, string name, List<string> playerIds)
    {
        var success = await _service.UpdateEncounter(lobbyId, encounterId, name, playerIds);
        if (!success)
        {
            ShowError("Failed to update encounter.");
        }
    }

    public async Task ReverseTurn(string encounterId)
    {
        var success = await _service.ReverseTurn(encounterId);
        if (!success)
        {
            ShowError("Failed to reverse turn.");
        }
    }

    public async Task AdvanceTurn(string encounterId)
    {
        var success = await _service.AdvanceTurn(encounterId);
        if (!success)
        {
            ShowError("Failed to advance turn.");
        }
    }

    public async Task SetInitiative(string encounterId, string participantId, int value)
    {
        var success = await _service.SetInitiative(encounterId, participantId, value);
        if (!success)
        {
            ShowError("Failed to set initiative.");
        }
    }

    public async Task AddNpcParticipant(string encounterId, string displayName)
    {
        var success = await _service.AddNpcParticipant(encounterId, displayName);
        if (!success)
        {
            ShowError("Failed to add NPC.");
        }
    }

    public async Task RemoveNpcParticipant(string encounterId, string participantId)
    {
        var success = await _service.RemoveNpcParticipant(encounterId, participantId);
        if (!success)
        {
            ShowError("Failed to remove NPC.");
        }
    }

    public async Task RenameNpcParticipant(string encounterId, string participantId, string newDisplayName)
    {
        var success = await _service.RenameNpcParticipant(encounterId, participantId, newDisplayName);
        if (!success)
        {
            ShowError("Failed to rename NPC.");
        }
    }

    public async Task EndEncounter(string encounterId)
    {
        var success = await _service.EndEncounter(encounterId);
        if (!success)
        {
            ShowError("Failed to end encounter.");
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

    private static void ShowError(string message)
    {
        Plugin.NotificationManager.AddNotification(new Notification
        {
            Content = message,
            Type = NotificationType.Error,
        });
    }

    public void Dispose()
    {
        _service.OnEncounterStateUpdated -= OnEncounterStateUpdated;
        _service.OnEncounterEnded -= OnEncounterEnded;
    }
}
