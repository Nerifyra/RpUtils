using System.Collections.Generic;

namespace RpUtils.Features.Encounters.Models;

public class EncounterState
{
    public string EncounterId { get; set; } = string.Empty;
    public string LobbyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RoundNumber { get; set; }
    public List<EncounterParticipant> Participants { get; set; } = [];
}

public class EncounterParticipant
{
    public string ParticipantId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int? Initiative { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsNpc { get; set; }
}