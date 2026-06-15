using RpUtils.Features.Markers.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Markers;

public interface IMarkersController
{
    IReadOnlyDictionary<string, Marker> Markers { get; }
    Marker? PlacingMarker { get; }

    event Action? OnStateChanged;

    Task AddMarker(string lobbyId, uint iconId);
    Task UpdateMarker(Marker marker);
    Task RemoveMarker(string id);
    Task RefreshMarkers(string lobbyId);

    void BeginPlacement(Marker marker);
    void CancelPlacement();
}
