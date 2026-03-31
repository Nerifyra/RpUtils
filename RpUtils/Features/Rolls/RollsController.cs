using Dalamud.Interface.ImGuiNotification;
using RpUtils.Features.Rolls.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.Features.Rolls;

public sealed class RollsController : IRollsController, IDisposable
{
    private readonly RollsService _service;
    private readonly Dictionary<string, RollRequestState> _rollRequests = [];

    public IReadOnlyDictionary<string, RollRequestState> RollRequests => _rollRequests;

    public event Action? OnStateChanged;

    public RollsController(RollsService service)
    {
        _service = service;

        _service.OnRollRequestStateUpdated += OnRollRequestStateUpdated;
        _service.OnRollRequestClosed += OnRollRequestClosedHandler;
    }

    private void OnRollRequestStateUpdated(RollRequestState state)
    {
        var isNew = !_rollRequests.ContainsKey(state.RollRequestId);
        var previous = isNew ? null : _rollRequests[state.RollRequestId];

        _rollRequests[state.RollRequestId] = state;
        OnStateChanged?.Invoke();

        RollChatEcho.OnRollUpdate(state, isNew, previous);
    }

    private void OnRollRequestClosedHandler(string rollRequestId)
    {
        _rollRequests.Remove(rollRequestId);
        OnStateChanged?.Invoke();
    }

    public async Task CreateRollRequest(string encounterId, string name, int? dc, bool isInitiativeRoll, List<string> participantIds)
    {
        var success = await _service.CreateRollRequest(encounterId, name, dc, isInitiativeRoll, participantIds);
        if (!success)
        {
            Plugin.NotificationManager.AddNotification(new Notification
            {
                Content = "Failed to create roll request.",
                Type = NotificationType.Error,
            });
        }
    }

    public async Task SubmitRoll(string rollRequestId, string participantId, int value)
    {
        var success = await _service.SubmitRoll(rollRequestId, participantId, value);
        if (!success)
        {
            Plugin.NotificationManager.AddNotification(new Notification
            {
                Content = "Failed to submit roll.",
                Type = NotificationType.Error,
            });
        }
    }

    public async Task EndRollRequest(string rollRequestId)
    {
        await _service.EndRollRequest(rollRequestId);
    }

    public async Task CloseRollRequest(string rollRequestId)
    {
        await _service.CloseRollRequest(rollRequestId);
    }

    public async Task RefreshEncounterRolls(string encounterId)
    {
        var rolls = await _service.GetEncounterRolls(encounterId);
        if (rolls == null) return;

        // Remove old rolls for this encounter
        var toRemove = _rollRequests.Where(kvp => kvp.Value.EncounterId == encounterId).Select(kvp => kvp.Key).ToList();
        foreach (var key in toRemove)
            _rollRequests.Remove(key);

        // Add fresh ones
        foreach (var roll in rolls)
            _rollRequests[roll.RollRequestId] = roll;

        OnStateChanged?.Invoke();
    }

    /// <summary>
    /// Gets all active roll requests for a given encounter, ordered by creation time.
    /// </summary>
    public List<RollRequestState> GetRollsForEncounter(string encounterId)
    {
        return _rollRequests.Values
            .Where(r => r.EncounterId == encounterId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToList();
    }

    public void Dispose()
    {
        _service.OnRollRequestStateUpdated -= OnRollRequestStateUpdated;
        _service.OnRollRequestClosed -= OnRollRequestClosedHandler;
    }

}
