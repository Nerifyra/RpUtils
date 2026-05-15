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
    public event Action<Guid>? OnMarkerRemoved;

    public MarkersService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<Marker>("MarkerUpdated", marker => OnMarkerUpdated?.Invoke(marker));
            connection.On<Guid>("MarkerRemoved", id => OnMarkerRemoved?.Invoke(id));
        };
    }

    public async Task<bool> UpdateMarker(Marker marker)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("UpdateMarker", marker);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to update marker.");
            return false;
        }
    }

    public async Task<bool> RemoveMarker(Guid markerId)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("RemoveMarker", markerId);
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove marker.");
            return false;
        }
    }

    public async Task<List<Marker>?> GetLobbyMarkers(string lobbyId)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            return await _hub.Connection!.InvokeAsync<List<Marker>>("GetLobbyMarkers", lobbyId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get lobby markers.");
            return null;
        }
    }
}
