using RpUtils.Features.Rolls.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Rolls;

public interface IRollsController
{
    IReadOnlyDictionary<string, RollRequestState> RollRequests { get; }

    event Action? OnStateChanged;

    Task CreateRollRequest(string encounterId, string name, int? dc, bool isInitiativeRoll, List<string> participantIds);
    Task SubmitRoll(string rollRequestId, string participantId, int value);
    Task EndRollRequest(string rollRequestId);
    Task CloseRollRequest(string rollRequestId);
    Task RefreshEncounterRolls(string encounterId);
    List<RollRequestState> GetRollsForEncounter(string encounterId);
}
