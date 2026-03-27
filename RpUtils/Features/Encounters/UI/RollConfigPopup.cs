using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.Models;
using RpUtils.Features.Lobbies.Models;
using System.Collections.Generic;
using System.Linq;

namespace RpUtils.Features.Encounters.UI;

internal class RollConfigPopup
{
    private string _name = string.Empty;
    private string _dcBuffer = string.Empty;
    private bool _isInitiativeRoll;
    private readonly HashSet<string> _selectedParticipantIds = [];
    private bool _openPopup;
    private string _encounterId = string.Empty;

    public void Open(string encounterId, EncounterState encounter)
    {
        _encounterId = encounterId;
        _name = string.Empty;
        _dcBuffer = string.Empty;
        _isInitiativeRoll = false;
        _selectedParticipantIds.Clear();
        _selectedParticipantIds.UnionWith(encounter.Participants.Select(p => p.ParticipantId));
        _openPopup = true;
    }

    public void Draw(EncounterState encounter)
    {
        if (_openPopup)
        {
            ImGui.OpenPopup($"RollConfig##{_encounterId}");
            _openPopup = false;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 0), ImGuiCond.Always);
        using var popup = ImRaii.Popup($"RollConfig##{_encounterId}");
        if (!popup.Success) return;

        ImGui.PushTextWrapPos(0);
        ImGui.TextDisabled("Initiating a roll request will listen to your chat for rolls from the requested players, and attempt to automatically populate the results.");
        ImGui.PopTextWrapPos();

        ImGui.Spacing();

        // Name
        ImGui.Text("Roll Name (optional)");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##RollName{_encounterId}", ref _name, 64);

        ImGui.Spacing();

        // Initiative toggle
        ImGui.Checkbox($"Initiative Roll##{_encounterId}", ref _isInitiativeRoll);
        if (_isInitiativeRoll)
            _dcBuffer = string.Empty;

        ImGui.Spacing();

        // DC
        using (ImRaii.Disabled(_isInitiativeRoll))
        {
            ImGui.Text("DC (optional)");
            ImGui.SetNextItemWidth(-1);
            ImGui.InputText($"##RollDC{_encounterId}", ref _dcBuffer, 8, ImGuiInputTextFlags.CharsDecimal);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Participant selector
        ImGui.Text("Participants");
        var participants = encounter.Participants;
        var height = participants.Count * ImGui.GetFrameHeightWithSpacing();
        using (var child = ImRaii.Child($"RollParticipants##{_encounterId}", new System.Numerics.Vector2(0, height), true))
        {
            if (child.Success)
            {
                foreach (var participant in participants)
                {
                    var isSelected = _selectedParticipantIds.Contains(participant.ParticipantId);
                    if (ImGui.Checkbox($"{participant.DisplayName}##{participant.ParticipantId}", ref isSelected))
                    {
                        if (isSelected)
                            _selectedParticipantIds.Add(participant.ParticipantId);
                        else
                            _selectedParticipantIds.Remove(participant.ParticipantId);
                    }
                }
            }
        }

        ImGui.Spacing();

        var canCreate = _selectedParticipantIds.Count > 0;
        using (ImRaii.Disabled(!canCreate))
        {
            if (ImGui.Button("Start Roll", new System.Numerics.Vector2(-1, 0)))
            {
                int? dc = null;
                if (int.TryParse(_dcBuffer, out var parsedDc))
                    dc = parsedDc;

                Plugin.Rolls.CreateRollRequest(
                    _encounterId,
                    _name,
                    dc,
                    _isInitiativeRoll,
                    _selectedParticipantIds.ToList()
                );

                ImGui.CloseCurrentPopup();
            }
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
