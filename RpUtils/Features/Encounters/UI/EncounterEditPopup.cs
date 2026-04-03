using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.Models;
using RpUtils.Features.Lobbies.Models;
using System.Linq;

namespace RpUtils.Features.Encounters.UI;

internal class EncounterEditPopup
{
    private readonly string _lobbyId;
    private readonly ParticipantSelector _participantSelector;
    private string _encounterName = string.Empty;
    private string? _encounterId;
    private bool _openPopup;

    private bool IsEditing => _encounterId != null;

    public EncounterEditPopup(string lobbyId)
    {
        _lobbyId = lobbyId;
        _participantSelector = new ParticipantSelector(lobbyId);
    }

    public void Open(Lobby lobby)
    {
        _encounterId = null;
        _encounterName = string.Empty;
        _participantSelector.SelectAll(lobby.State.Members.Select(m => m.PlayerId));
        _openPopup = true;
    }

    public void OpenForEdit(Lobby lobby, EncounterState encounter)
    {
        _encounterId = encounter.EncounterId;
        _encounterName = encounter.Name;
        _participantSelector.SelectAll(encounter.Participants.Select(p => p.PlayerId));
        _openPopup = true;
    }

    public void Draw(Lobby lobby)
    {
        if (_openPopup)
        {
            ImGui.OpenPopup($"EncounterEditPopup##{_lobbyId}");
            _openPopup = false;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 350), ImGuiCond.Appearing);
        using var popup = ImRaii.Popup($"EncounterEditPopup##{_lobbyId}", ImGuiWindowFlags.None);
        if (!popup.Success) return;

        ImGui.Text("Encounter Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##EncounterName{_lobbyId}", ref _encounterName, 64);

        ImGui.Spacing();
        ImGui.Text("Participants");

        _participantSelector.Draw(lobby.State.Members);

        ImGui.Spacing();

        var canSave = _participantSelector.SelectedPlayerIds.Count > 0;
        using (ImRaii.Disabled(!canSave))
        {
            var buttonLabel = IsEditing ? "Save" : "Create";
            if (ImGui.Button(buttonLabel, new System.Numerics.Vector2(-1, 0)))
            {
                var name = string.IsNullOrWhiteSpace(_encounterName) ? "Encounter" : _encounterName;
                var playerIds = _participantSelector.SelectedPlayerIds.ToList();

                if (IsEditing)
                    Plugin.Encounters.UpdateEncounter(_lobbyId, _encounterId!, name, playerIds);
                else
                    Plugin.Encounters.CreateEncounter(_lobbyId, name, playerIds);

                ImGui.CloseCurrentPopup();
            }
        }

        if (ImGui.Button("Cancel", new System.Numerics.Vector2(-1, 0)))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
