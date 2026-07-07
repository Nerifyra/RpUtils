using RpUtils.Features.Rolls.Models;
using RpUtils.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace RpUtils.Features.Rolls;

public sealed class RollsController : IRollsController, IDisposable
{
    private readonly RollsService _service;
    private readonly ConcurrentDictionary<string, RollRequestState> _rollRequests = new();

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
        var isNew = !_rollRequests.TryGetValue(state.RollRequestId, out var previous);

        _rollRequests[state.RollRequestId] = state;
        OnStateChanged?.Invoke();

        if (isNew)
        {
            RollAlerts.AlertRollRequested(state);
            return;
        }

        if (IsJustCompleted(previous!, state))
            RollAlerts.AlertRollCompleted(state);
    }

    private static bool IsJustCompleted(RollRequestState previous, RollRequestState current)
    {
        var wasAllResolved = previous.Participants.All(p => !p.IsPending);
        var isAllResolved = current.Participants.All(p => !p.IsPending);
        var justEnded = previous.IsActive && !current.IsActive;
        return (isAllResolved && !wasAllResolved) || justEnded;
    }

    private void OnRollRequestClosedHandler(string rollRequestId)
    {
        _rollRequests.TryRemove(rollRequestId, out _);
        OnStateChanged?.Invoke();
    }

    public async Task CreateRollRequest(string encounterId, string name, int? dc, bool isInitiativeRoll, List<string> participantIds)
    {
        var success = await _service.CreateRollRequest(encounterId, name, dc, isInitiativeRoll, participantIds);
        if (!success) Notify.Error("Failed to create roll request.");
    }

    public async Task SubmitRoll(string rollRequestId, string participantId, int value)
    {
        var success = await _service.SubmitRoll(rollRequestId, participantId, value);
        if (!success) Notify.Error("Failed to submit roll.");
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
            _rollRequests.TryRemove(key, out _);

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


    public async void GenerateRoll(string args)
    {
        if (!Chat.CurrentChannelCanMessage())
        {
            Notify.Warning("Current channel disallowed from rolling.");
            return;
        }

        var result = await _service.GenerateRoll(args);
        if (result.Value is not { } roll)
        {
            Chat.Echo(result.Error ?? "Couldn't roll.");
            return;
        }

        // Keep the public line short — the breakdown can get long, so it's left to /rollcheck.
        var message = $"({roll.Id}): {roll.Expression} = {roll.Result}";

        // Chat touches game memory, so it must run on the framework thread (we're off it after the await).
        await Plugin.Framework.RunOnFrameworkThread(() =>
        {
            if (Chat.SendMessage(message))
                return;

            // Channel changed during the round-trip — keep the result local so the roll isn't lost.
            EchoRoll(roll);
            Notify.Warning("Current channel disallowed from rolling.");
        });
    }

    public async void RollCheck(string args)
    {
        var roll = await _service.RollCheck(args);
        if (roll == null)
        {
            Chat.Echo($"No roll found for '{args}'. Usage: `/rollcheck ######`");
            return;
        }

        EchoRoll(roll);
    }

    // Local echo with the full breakdown — used by /rollcheck and the barred-channel fallback.
    private static void EchoRoll(GeneratedRoll roll) =>
        Chat.Echo($"{roll.Id}: {roll.Expression} → {roll.Breakdown} = {roll.Result}");

}
