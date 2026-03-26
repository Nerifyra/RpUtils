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
            Plugin.Log.Info("Updated encounter in lobby {LobbyId}", lobbyId);
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

    public async Task<bool> EndEncounter(string encounterId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("EndEncounter", encounterId);
            Plugin.Log.Info("Ended encounter {EncounterId}", encounterId);
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
