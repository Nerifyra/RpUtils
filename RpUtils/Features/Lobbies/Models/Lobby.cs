using System.Linq;

namespace RpUtils.Features.Lobbies.Models;

public class Lobby
{
    public string LobbyId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public LobbyState State { get; set; } = new();

    public bool IsOwner => State.Members.Any(m => m.PlayerId == PlayerId && m.IsOwner);
}
