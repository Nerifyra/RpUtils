using RpUtils.Features.Encounters.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Encounters;

public interface IEncountersController
{
    IReadOnlyDictionary<string, EncounterState> Encounters { get; }

    event Action? OnStateChanged;

    Task CreateEncounter(string lobbyId, string name, List<string> playerIds);
    Task UpdateEncounter(string lobbyId, string encounterId, string name, List<string> playerIds);
    Task AdvanceTurn(string encounterId);
    Task ReverseTurn(string encounterId);
    Task SetInitiative(string encounterId, string participantId, int value);
    Task AddNpcParticipant(string encounterId, string displayName);
    Task RemoveNpcParticipant(string encounterId, string participantId);
    Task RenameNpcParticipant(string encounterId, string participantId, string newDisplayName);
    Task EndEncounter(string encounterId);
    Task RefreshEncounters(string lobbyId);
}
