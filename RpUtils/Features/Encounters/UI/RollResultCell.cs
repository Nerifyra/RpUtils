using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Rolls.Models;
using System.Collections.Generic;
using System.Numerics;

using Theme = RpUtils.UI.Theme;

namespace RpUtils.Features.Encounters.UI;

internal static class RollResultCell
{
    private static readonly Dictionary<string, int> _buffers = [];
    private static readonly HashSet<string> _activeInputs = [];

    public static void Draw(string encounterId, string rollRequestId, RollParticipant? rollParticipant, int? dc, bool isActive, bool isDm)
    {
        if (rollParticipant == null)
        {
            ImGui.TextDisabled("-");
            return;
        }

        var key = $"{rollRequestId}_{rollParticipant.ParticipantId}";
        var serverValue = rollParticipant.Result ?? 0;

        // Determine cell background color
        Vector4 bgColor;
        if (rollParticipant.IsPending)
        {
            bgColor = Theme.PendingTint;
        }
        else if (dc.HasValue)
        {
            bgColor = rollParticipant.Result!.Value >= dc.Value
                ? Theme.SuccessTint
                : Theme.FailureTint;
        }
        else
        {
            bgColor = Theme.TransparentTint;
        }

        // Apply cell background
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(bgColor));

        if (!isDm)
        {
            // Non-DMs see text only
            if (rollParticipant.IsPending)
                ImGui.TextDisabled("...");
            else
                ImGui.Text(rollParticipant.Result!.Value.ToString());
            return;
        }

        // DMs get an editable input
        if (!_activeInputs.Contains(key))
            _buffers[key] = serverValue;

        var value = _buffers[key];
        ImGui.SetNextItemWidth(-1);

        if (ImGui.InputInt($"##Roll{key}", ref value, 0, 0))
        {
            _buffers[key] = value;
        }

        if (ImGui.IsItemActive())
            _activeInputs.Add(key);

        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            _activeInputs.Remove(key);
            if (_buffers[key] != serverValue)
            {
                Plugin.Rolls.SubmitRoll(rollRequestId, rollParticipant.ParticipantId, _buffers[key]);
            }
        }
        else if (!ImGui.IsItemActive())
        {
            _activeInputs.Remove(key);
        }
    }
}
