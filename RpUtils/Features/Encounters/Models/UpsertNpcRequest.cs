namespace RpUtils.Features.Encounters.Models;

public sealed record UpsertNpcRequest(
    string? ParticipantId,
    string? DisplayName,
    bool? IsVisible);
