using Dalamud.Interface.ImGuiNotification;
using RpUtils.Features.Lobbies.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Lobbies;

public sealed class LobbiesController : ILobbiesController, IDisposable
{
    private readonly LobbiesService _service;

    private readonly Dictionary<string, Lobby> _lobbies = [];
    private bool _isLoading;

    public IReadOnlyDictionary<string, Lobby> Lobbies => _lobbies;
    public bool IsLoading => _isLoading;

    public event Action? OnStateChanged;
    public event Action<string>? OnLobbyEntered;
    public event Action<string>? OnLobbyRemoved;

    public LobbiesController(LobbiesService service)
    {
        _service = service;

        _service.OnLobbyStateUpdated += OnLobbyStateUpdated;
        _service.OnLobbyClosed += OnLobbyClosed;
        _service.OnKickedFromLobby += OnKickedFromLobby;
    }

    private void OnLobbyStateUpdated(LobbyState state)
    {
        if (_lobbies.TryGetValue(state.LobbyId, out var lobby))
        {
            lobby.State = state;
        }

        OnStateChanged?.Invoke();
    }

    private void OnLobbyClosed(string lobbyId)
    {
        _lobbies.Remove(lobbyId);
        OnLobbyRemoved?.Invoke(lobbyId);
        OnStateChanged?.Invoke();
    }

    private void OnKickedFromLobby(string lobbyId)
    {
        _lobbies.Remove(lobbyId);
        OnLobbyRemoved?.Invoke(lobbyId);
        OnStateChanged?.Invoke();
    }

    public async Task CreateLobby()
    {
        var characterName = GetCharacterName();
        var result = await _service.CreateLobby(characterName);
        if (result is null)
        {
            ShowError("Failed to create lobby.");
            return;
        }

        _lobbies[result.LobbyId] = result;
        OnLobbyEntered?.Invoke(result.LobbyId);
        OnStateChanged?.Invoke();
    }

    public async Task JoinLobby(string joinCode)
    {
        var characterName = GetCharacterName();
        var result = await _service.JoinLobby(joinCode, characterName);
        if (result is null)
        {
            ShowError("Failed to join lobby.");
            return;
        }

        _lobbies[result.LobbyId] = result;
        OnLobbyEntered?.Invoke(result.LobbyId);
        OnStateChanged?.Invoke();
    }

    public async Task LeaveLobby(string lobbyId)
    {
        await _service.LeaveLobby(lobbyId);

        _lobbies.Remove(lobbyId);
        OnLobbyRemoved?.Invoke(lobbyId);
        OnStateChanged?.Invoke();
    }

    public async Task CloseLobby(string lobbyId)
    {
        await _service.CloseLobby(lobbyId);

        _lobbies.Remove(lobbyId);
        OnLobbyRemoved?.Invoke(lobbyId);
        OnStateChanged?.Invoke();
    }

    public async Task RegenerateJoinCode(string lobbyId)
    {
        await _service.RegenerateJoinCode(lobbyId);
    }

    public async Task RenameLobby(string lobbyId, string newName)
    {
        await _service.RenameLobby(lobbyId, newName);
    }

    public async Task KickMember(string lobbyId, string playerId)
    {
        await _service.KickMember(lobbyId, playerId);
    }

    public async Task TransferOwnership(string lobbyId, string playerId)
    {
        await _service.TransferOwnership(lobbyId, playerId);
    }

    public async Task PromoteMember(string lobbyId, string playerId)
    {
        await _service.PromoteMember(lobbyId, playerId);
    }

    public async Task DemoteMember(string lobbyId, string playerId)
    {
        await _service.DemoteMember(lobbyId, playerId);
    }

    public async Task UpdateMemberDisplayName(string lobbyId, string playerId, string newDisplayName)
    {
        await _service.UpdateMemberDisplayName(lobbyId, playerId, newDisplayName);
    }

    public async Task UpdateMemberCharacterName(string lobbyId, string playerId, string newCharacterName)
    {
        await _service.UpdateMemberCharacterName(lobbyId, playerId, newCharacterName);
    }

    public async Task CreateGhostPlayer(string lobbyId, string displayName, string characterName)
    {
        await _service.CreateGhostPlayer(lobbyId, displayName, characterName);
    }

    public async Task RemoveGhostPlayer(string lobbyId, string playerId)
    {
        await _service.RemoveGhostPlayer(lobbyId, playerId);
    }

    public async Task RefreshLobbies()
    {
        if (_isLoading) return;

        _isLoading = true;
        OnStateChanged?.Invoke();

        try
        {
            var lobbies = await _service.GetMyLobbies();
            if (lobbies is not null)
            {
                _lobbies.Clear();
                foreach (var lobby in lobbies)
                {
                    _lobbies[lobby.LobbyId] = lobby;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to refresh lobbies.");
        }
        finally
        {
            _isLoading = false;
            OnStateChanged?.Invoke();
        }
    }

    private static string GetCharacterName()
    {
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer is null) return "Unknown";

        var name = localPlayer.Name.ToString();
        var world = Plugin.PlayerState.CurrentWorld.Value.Name.ToString();
        return $"{name}@{world}";
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
        _service.OnLobbyStateUpdated -= OnLobbyStateUpdated;
        _service.OnLobbyClosed -= OnLobbyClosed;
        _service.OnKickedFromLobby -= OnKickedFromLobby;
    }
}
