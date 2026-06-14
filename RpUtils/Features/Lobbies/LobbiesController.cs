using RpUtils.Features.Lobbies.Models;
using RpUtils.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Linq;
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
        Plugin.ConnectionStatus.OnStatusChanged += OnConnectionStateChanged;
    }

    private void OnConnectionStateChanged(ConnectionState state)
    {
        if (state == ConnectionState.Connected)
            _ = RefreshLobbies();
        else if (state is ConnectionState.Disconnected or ConnectionState.Disabled)
            Clear();
    }

    private void Clear()
    {
        _lobbies.Clear();
        OnStateChanged?.Invoke();
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
            Notify.Error("Failed to create lobby.");
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
            Notify.Error("Failed to join lobby.");
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
                var newIds = lobbies.Select(l => l.LobbyId).ToHashSet();
                foreach (var goneId in _lobbies.Keys.Where(id => !newIds.Contains(id)).ToList())
                {
                    _lobbies.Remove(goneId);
                    OnLobbyRemoved?.Invoke(goneId);
                }
                foreach (var lobby in lobbies)
                {
                    var isNew = !_lobbies.ContainsKey(lobby.LobbyId);
                    _lobbies[lobby.LobbyId] = lobby;
                    if (isNew) OnLobbyEntered?.Invoke(lobby.LobbyId);
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

    public (string DisplayName, string CharacterName)? GetTargetPlayerInfo()
    {
        if (Plugin.ObjectTable.LocalPlayer?.TargetObject is not Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter target)
            return null;

        var fullName = target.Name.TextValue;
        var world = target.HomeWorld.Value.Name.ToString();
        var displayName = fullName.Split(' ')[0];
        var characterName = $"{fullName}@{world}";
        return (displayName, characterName);
    }

    private static string GetCharacterName()
    {
        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer is null) return "Unknown";

        var name = localPlayer.Name.ToString();
        var world = Plugin.PlayerState.HomeWorld.Value.Name.ToString();
        return $"{name}@{world}";
    }

    public void Dispose()
    {
        _service.OnLobbyStateUpdated -= OnLobbyStateUpdated;
        _service.OnLobbyClosed -= OnLobbyClosed;
        _service.OnKickedFromLobby -= OnKickedFromLobby;
        Plugin.ConnectionStatus.OnStatusChanged -= OnConnectionStateChanged;
    }
}
