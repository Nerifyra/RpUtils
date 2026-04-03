using Microsoft.AspNetCore.SignalR.Client;
using RpUtils.Features.Rolls.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Rolls;

public sealed class RollsService
{
    private readonly HubConnectionService _hub;

    public event Action<RollRequestState>? OnRollRequestStateUpdated;
    public event Action<string>? OnRollRequestClosed;

    public RollsService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            connection.On<RollRequestState>("RollRequestStateUpdated", state => OnRollRequestStateUpdated?.Invoke(state));
            connection.On<string>("RollRequestClosed", rollRequestId => OnRollRequestClosed?.Invoke(rollRequestId));
        };
    }

    public async Task<bool> CreateRollRequest(string encounterId, string name, int? dc, bool isInitiativeRoll, List<string> participantIds)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("CreateRollRequest", encounterId, name, dc, isInitiativeRoll, participantIds);
            Plugin.Log.Debug($"Created roll request for encounter {encounterId}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to create roll request.");
            return false;
        }
    }

    public async Task<bool> SubmitRoll(string rollRequestId, string participantId, int value)
    {
        try
        {
            if (!_hub.IsConnected) return false;
            await _hub.Connection!.InvokeAsync("SubmitRoll", rollRequestId, participantId, value);
            Plugin.Log.Debug($"Submitted roll for {participantId} in roll request {rollRequestId}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to submit roll.");
            return false;
        }
    }

    public async Task EndRollRequest(string rollRequestId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("EndRollRequest", rollRequestId);
            Plugin.Log.Debug($"Ended roll request {rollRequestId}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to end roll request.");
        }
    }

    public async Task CloseRollRequest(string rollRequestId)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("CloseRollRequest", rollRequestId);
            Plugin.Log.Debug($"Closed roll request {rollRequestId}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to close roll request.");
        }
    }

    public async Task<List<RollRequestState>?> GetEncounterRolls(string encounterId)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            return await _hub.Connection!.InvokeAsync<List<RollRequestState>>("GetEncounterRolls", encounterId);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get encounter rolls.");
            return null;
        }
    }
}
