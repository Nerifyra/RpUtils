using System.Collections.Generic;

namespace RpUtils.Features.Lobbies.Models;

public class LobbyState
{
    public string LobbyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string JoinCode { get; set; } = string.Empty;
    public List<LobbyMember> Members { get; set; } = [];
}

public class LobbyMember
{
    public string PlayerId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public bool IsOwner => Role == nameof(LobbyRole.Owner);
    public bool IsModerator => Role == nameof(LobbyRole.Moderator);
    public bool IsGhost => Role == nameof(LobbyRole.Ghost);
    public bool IsModeratorOrAbove => IsModerator || IsOwner;
}
