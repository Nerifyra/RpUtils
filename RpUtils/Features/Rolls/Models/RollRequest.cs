using System.Collections.Generic;

namespace RpUtils.Features.Rolls.Models;

public class RollRequestState
{
    public string RollRequestId { get; set; } = string.Empty;
    public string EncounterId { get; set; } = string.Empty;
    public string LobbyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? DC { get; set; }
    public bool IsInitiativeRoll { get; set; }
    public bool IsActive { get; set; }
    public string CreatedByPlayerId { get; set; } = string.Empty;
    public long CreatedAtUtc { get; set; }
    public List<RollParticipant> Participants { get; set; } = [];
}

public class RollParticipant
{
    public string ParticipantId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public bool IsNpc { get; set; }
    public int? Result { get; set; }
    public bool IsPending => Result == null;
}
