using System.Numerics;
using System.Text.Json.Serialization;

namespace RpUtils.Features.Markers.Models;

public class Marker
{
    public string Id { get; set; } = string.Empty;
    public string LobbyId { get; set; } = string.Empty;
    public uint TerritoryType { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public uint IconId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsPlaced { get; set; }
    public bool IsVisible { get; set; } = false;
    public float Size { get; set; } = 1f;
    public uint Color { get; set; } = 0xFFFFFFFFu;

    public string? NpcParticipantId { get; set; }

    [JsonIgnore]
    public Vector3 WorldPos
    {
        get => new(PosX, PosY, PosZ);
        set { PosX = value.X; PosY = value.Y; PosZ = value.Z; }
    }
}
