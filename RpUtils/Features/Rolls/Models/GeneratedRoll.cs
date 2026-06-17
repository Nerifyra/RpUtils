namespace RpUtils.Features.Rolls.Models;

/// <summary>
/// A server-generated, verifiable dice roll. Mirrors the server's GeneratedRoll: the server
/// owns the RNG and stores this under <see cref="Id"/> so it can be confirmed later.
/// </summary>
public class GeneratedRoll
{
    public string Id { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string Breakdown { get; set; } = string.Empty;
    public int Result { get; set; }
    public long CreatedAtUtc { get; set; }
}
