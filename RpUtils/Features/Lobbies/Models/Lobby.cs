using System.Linq;

namespace RpUtils.Features.Lobbies.Models;

public class Lobby
{
    public string LobbyId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public LobbyState State { get; set; } = new();

    public LobbyMember? MyMember => State.Members.FirstOrDefault(m => m.PlayerId == PlayerId);
    public bool IsOwner => MyMember?.IsOwner ?? false;
    public bool IsModeratorOrAbove => MyMember?.IsModeratorOrAbove ?? false;
}
