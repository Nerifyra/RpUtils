using Microsoft.AspNetCore.SignalR.Client;
using RpUtils.Features.Markers.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Markers;

public sealed class MarkersService
{
    private readonly HubConnectionService _hub;

    public event Action<Marker>? OnMarkerUpdated;
    public event Action<string>? OnMarkerRemoved;

    public MarkersService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<Marker>("MarkerUpdated", marker => OnMarkerUpdated?.Invoke(marker));
            connection.On<string>("MarkerRemoved", id => OnMarkerRemoved?.Invoke(id));
        };
    }

    public Task<Result> UpdateMarker(Marker marker) =>
        _hub.InvokeResult("UpdateMarker", marker);

    public Task<Result> RemoveMarker(string markerId) =>
        _hub.InvokeResult("RemoveMarker", markerId);

    public Task<Result<List<Marker>>> GetLobbyMarkers(string lobbyId) =>
        _hub.InvokeResult<List<Marker>>("GetLobbyMarkers", lobbyId);
}
