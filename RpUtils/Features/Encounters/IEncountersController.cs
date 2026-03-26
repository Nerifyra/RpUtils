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
    Task RefreshEncounters(string lobbyId);
}
