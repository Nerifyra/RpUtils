using Microsoft.AspNetCore.SignalR.Client;
using RpUtils.Features.Encounters.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Encounters;

public sealed class EncountersService
{
    private readonly HubConnectionService _hub;

    public event Action<EncounterState>? OnEncounterStateUpdated;
    public event Action<string>? OnEncounterEnded;

    public EncountersService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<EncounterState>("EncounterStateUpdated", state => OnEncounterStateUpdated?.Invoke(state));
            connection.On<string>("EncounterEnded", encounterId => OnEncounterEnded?.Invoke(encounterId));
        };
    }

    public async Task<bool> UpdateEncounter(string lobbyId, string? encounterId, string name, List<string> playerIds)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("UpdateEncounter", lobbyId, encounterId, name, playerIds);
            Plugin.Log.Debug($"Updated encounter in lobby {lobbyId}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to update encounter.");
            return false;
        }
    }

    public async Task<bool> ReverseTurn(string encounterId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("ReverseTurn", encounterId);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to reverse turn.");
            return false;
        }
    }

    public async Task<bool> AdvanceTurn(string encounterId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("AdvanceTurn", encounterId);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to advance turn.");
            return false;
        }
    }

    public async Task<bool> SetInitiative(string encounterId, string participantId, int value)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("SetInitiative", encounterId, participantId, value);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to set initiative.");
            return false;
        }
    }

    public async Task<bool> AddNpcParticipant(string encounterId, string displayName)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("AddNpcParticipant", encounterId, displayName);
            Plugin.Log.Debug($"Added NPC to encounter {encounterId}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to add NPC participant.");
            return false;
        }
    }

    public async Task<bool> RemoveNpcParticipant(string encounterId, string participantId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("RemoveNpcParticipant", encounterId, participantId);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove NPC participant.");
            return false;
        }
    }

    public async Task<bool> RenameNpcParticipant(string encounterId, string participantId, string newDisplayName)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("RenameNpcParticipant", encounterId, participantId, newDisplayName);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to rename NPC participant.");
            return false;
        }
    }

    public async Task<bool> EndEncounter(string encounterId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("EndEncounter", encounterId);
            Plugin.Log.Debug($"Ended encounter {encounterId}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to end encounter.");
            return false;
        }
    }

    public async Task<List<EncounterState>?> GetLobbyEncounters(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<List<EncounterState>>("GetLobbyEncounters", lobbyId);
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get lobby encounters.");
            return null;
        }
    }
}
