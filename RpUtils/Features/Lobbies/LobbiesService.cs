using Microsoft.AspNetCore.SignalR.Client;
using RpUtils.Features.Lobbies.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Lobbies;

public sealed class LobbiesService
{
    private readonly HubConnectionService _hub;

    public event Action<LobbyState>? OnLobbyStateUpdated;
    public event Action<string>? OnLobbyClosed;
    public event Action<string>? OnKickedFromLobby;

    public LobbiesService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<LobbyState>("LobbyStateUpdated", state => OnLobbyStateUpdated?.Invoke(state));
            connection.On<string>("LobbyClosed", lobbyId => OnLobbyClosed?.Invoke(lobbyId));
            connection.On<string>("KickedFromLobby", lobbyId => OnKickedFromLobby?.Invoke(lobbyId));
        };
    }

    public async Task<Lobby?> CreateLobby(string characterName)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<Lobby>("CreateLobby", characterName);
            Plugin.Log.Info("Created lobby: {LobbyId}", result.LobbyId);
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to create lobby.");
            return null;
        }
    }

    public async Task<Lobby?> JoinLobby(string joinCode, string characterName)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<Lobby>("JoinLobby", joinCode, characterName);
            Plugin.Log.Info("Joined lobby: {LobbyId}", result.LobbyId);
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to join lobby.");
            return null;
        }
    }

    public async Task LeaveLobby(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("LeaveLobby", lobbyId);
            Plugin.Log.Info("Left lobby: {LobbyId}", lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to leave lobby.");
        }
    }

    public async Task<List<Lobby>?> GetMyLobbies()
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<List<Lobby>>("GetMyLobbies");
            Plugin.Log.Debug("Got {Count} lobbies.", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get lobbies.");
            return null;
        }
    }

    public async Task<LobbyState?> GetLobbyState(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<LobbyState>("GetLobbyState", lobbyId);
            Plugin.Log.Debug("Got lobby state: {LobbyId}", lobbyId);
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get lobby state.");
            return null;
        }
    }

    public async Task CloseLobby(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("CloseLobby", lobbyId);
            Plugin.Log.Info("Closed lobby: {LobbyId}", lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to close lobby.");
        }
    }

    public async Task KickMember(string lobbyId, string targetPlayerId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("KickMember", lobbyId, targetPlayerId);
            Plugin.Log.Info("Kicked player {PlayerId} from lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to kick member.");
        }
    }

    public async Task TransferOwnership(string lobbyId, string targetPlayerId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("TransferOwnership", lobbyId, targetPlayerId);
            Plugin.Log.Info("Transferred ownership of {LobbyId} to {PlayerId}", lobbyId, targetPlayerId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to transfer ownership.");
        }
    }

    public async Task PromoteMember(string lobbyId, string targetPlayerId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("PromoteMember", lobbyId, targetPlayerId);
            Plugin.Log.Info("Promoted player {PlayerId} in lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to promote member.");
        }
    }

    public async Task DemoteMember(string lobbyId, string targetPlayerId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("DemoteMember", lobbyId, targetPlayerId);
            Plugin.Log.Info("Demoted player {PlayerId} in lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to demote member.");
        }
    }

    public async Task RegenerateJoinCode(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("RegenerateJoinCode", lobbyId);
            Plugin.Log.Info("Regenerated join code for {LobbyId}", lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to regenerate join code.");
        }
    }

    public async Task RenameLobby(string lobbyId, string newName)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("RenameLobby", lobbyId, newName);
            Plugin.Log.Info("Renamed lobby {LobbyId} to {Name}", lobbyId, newName);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to rename lobby.");
        }
    }

    public async Task UpdateMemberDisplayName(string lobbyId, string targetPlayerId, string newDisplayName)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("UpdateMemberDisplayName", lobbyId, targetPlayerId, newDisplayName);
            Plugin.Log.Info("Updated display name for {PlayerId} in lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to update display name.");
        }
    }

    public async Task UpdateMemberCharacterName(string lobbyId, string targetPlayerId, string newCharacterName)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("UpdateMemberCharacterName", lobbyId, targetPlayerId, newCharacterName);
            Plugin.Log.Info("Updated character name for {PlayerId} in lobby {LobbyId}", targetPlayerId, lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to update character name.");
        }
    }
}
