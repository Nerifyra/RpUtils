using RpUtils.Features.Lobbies.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Lobbies;

public interface ILobbiesController
{
    IReadOnlyDictionary<string, Lobby> Lobbies { get; }
    bool IsLoading { get; }

    event Action? OnStateChanged;
    event Action<string>? OnLobbyEntered;
    event Action<string>? OnLobbyRemoved;

    Task CreateLobby();
    Task JoinLobby(string joinCode);
    Task LeaveLobby(string lobbyId);
    Task CloseLobby(string lobbyId);
    Task RegenerateJoinCode(string lobbyId);
    Task RenameLobby(string lobbyId, string newName);
    Task KickMember(string lobbyId, string playerId);
    Task TransferOwnership(string lobbyId, string playerId);
    Task UpdateMemberDisplayName(string lobbyId, string playerId, string newDisplayName);
    Task UpdateMemberCharacterName(string lobbyId, string playerId, string newCharacterName);
    Task RefreshLobbies();
}
