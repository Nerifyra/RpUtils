using RpUtils.Features.Markers.Models;
using RpUtils.Features.Markers.Rendering;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.Features.Markers;

public sealed class MarkersController : IMarkersController, IDisposable
{
    private readonly MarkersService _service;
    private readonly MarkerRenderer _renderer;
    private readonly MarkerPlacement _placement;
    private readonly Dictionary<string, Marker> _markers = [];

    public IReadOnlyDictionary<string, Marker> Markers => _markers;
    public Marker? PlacingMarker { get; private set; }

    public event Action? OnStateChanged;

    public MarkersController(MarkersService service)
    {
        _service = service;
        _renderer = new MarkerRenderer();
        _placement = new MarkerPlacement();

        _service.OnMarkerUpdated += OnMarkerUpdated;
        _service.OnMarkerRemoved += OnMarkerRemoved;
        Plugin.Lobbies.OnLobbyEntered += OnLobbyEntered;
        Plugin.Lobbies.OnLobbyRemoved += OnLobbyRemoved;
    }

    private void OnLobbyEntered(string lobbyId) => _ = RefreshMarkers(lobbyId);

    private void OnLobbyRemoved(string lobbyId)
    {
        if (PlacingMarker?.LobbyId == lobbyId) CancelPlacement();

        foreach (var id in _markers.Keys.Where(id => _markers[id].LobbyId == lobbyId).ToList())
            _markers.Remove(id);

        OnStateChanged?.Invoke();
    }

    private void OnMarkerUpdated(Marker marker)
    {
        _markers[marker.Id] = marker;
        OnStateChanged?.Invoke();
    }

    private void OnMarkerRemoved(string id)
    {
        if (PlacingMarker?.Id == id) CancelPlacement();
        _markers.Remove(id);
        OnStateChanged?.Invoke();
    }

    public async Task AddMarker(string lobbyId, uint iconId)
    {
        var marker = new Marker
        {
            Id = Guid.NewGuid().ToString(),
            LobbyId = lobbyId,
            IconId = iconId,
        };
        var success = await _service.UpdateMarker(marker);
        if (!success) Notify.Error("Failed to add marker.");
    }

    public async Task UpdateMarker(Marker marker)
    {
        var success = await _service.UpdateMarker(marker);
        if (!success) Notify.Error("Failed to update marker.");
    }

    public async Task RemoveMarker(string id)
    {
        var success = await _service.RemoveMarker(id);
        if (!success) Notify.Error("Failed to remove marker.");
    }

    public async Task RefreshMarkers(string lobbyId)
    {
        try
        {
            var markers = await _service.GetLobbyMarkers(lobbyId);
            if (markers is null) return;

            // Drop cached markers for this lobby and repopulate.
            foreach (var id in _markers.Keys.Where(id => _markers[id].LobbyId == lobbyId).ToList())
                _markers.Remove(id);

            foreach (var marker in markers)
                _markers[marker.Id] = marker;

            OnStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to refresh markers.");
        }
    }

    public void BeginPlacement(Marker marker) => PlacingMarker = marker;
    public void CancelPlacement() => PlacingMarker = null;

    public void Dispose()
    {
        _service.OnMarkerUpdated -= OnMarkerUpdated;
        _service.OnMarkerRemoved -= OnMarkerRemoved;
        Plugin.Lobbies.OnLobbyEntered -= OnLobbyEntered;
        Plugin.Lobbies.OnLobbyRemoved -= OnLobbyRemoved;
        _renderer.Dispose();
        _placement.Dispose();
    }
}
