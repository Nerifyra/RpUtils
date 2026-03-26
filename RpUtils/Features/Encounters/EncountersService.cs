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

    public EncountersService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<EncounterState>("EncounterStateUpdated", state => OnEncounterStateUpdated?.Invoke(state));
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
