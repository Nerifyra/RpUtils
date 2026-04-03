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
            Plugin.Log.Debug($"Created lobby: {result.LobbyId}");
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
            Plugin.Log.Debug($"Joined lobby: {result.LobbyId}");
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
            Plugin.Log.Debug($"Left lobby: {lobbyId}");
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
            Plugin.Log.Debug($"Got {result.Count} lobbies.");
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
            Plugin.Log.Debug($"Got lobby state: {lobbyId}");
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
            Plugin.Log.Debug($"Closed lobby: {lobbyId}");
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
            Plugin.Log.Debug($"Kicked player {targetPlayerId} from lobby {lobbyId}");
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
            Plugin.Log.Debug($"Transferred ownership of {lobbyId} to {targetPlayerId}");
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
            Plugin.Log.Debug($"Promoted player {targetPlayerId} in lobby {lobbyId}");
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
            Plugin.Log.Debug($"Demoted player {targetPlayerId} in lobby {lobbyId}");
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
            Plugin.Log.Debug($"Regenerated join code for {lobbyId}");
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
            Plugin.Log.Debug($"Renamed lobby {lobbyId} to {newName}");
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
            Plugin.Log.Debug($"Updated display name for {targetPlayerId} in lobby {lobbyId}");
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
            Plugin.Log.Debug($"Updated character name for {targetPlayerId} in lobby {lobbyId}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to update character name.");
        }
    }

    public async Task CreateGhostPlayer(string lobbyId, string displayName, string characterName)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("CreateGhostPlayer", lobbyId, displayName, characterName);
            Plugin.Log.Debug($"Created ghost player in lobby {lobbyId}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to create ghost player.");
        }
    }

    public async Task RemoveGhostPlayer(string lobbyId, string targetPlayerId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("RemoveGhostPlayer", lobbyId, targetPlayerId);
            Plugin.Log.Debug($"Removed ghost player {targetPlayerId} from lobby {lobbyId}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove ghost player.");
        }
    }
}
