using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.Models;
using RpUtils.Features.Lobbies.Models;
using System.Collections.Generic;
using System.Linq;

using Theme = RpUtils.UI.Theme;

namespace RpUtils.Features.Encounters.UI;

internal class EncounterDetailTab
{
    private readonly Dictionary<string, int> _initiativeBuffers = [];

    public void Draw(string encounterId, EncounterState encounter, Lobby lobby, EncounterEditPopup editPopup)
    {
        using var tab = ImRaii.TabItem($"{encounter.Name}##{encounterId}");
        if (!tab.Success) return;

        DrawControls(encounterId, encounter, lobby, editPopup);

        ImGui.Separator();

        DrawParticipantsTable(encounterId, encounter);
    }

    private void DrawControls(string encounterId, EncounterState encounter, Lobby lobby, EncounterEditPopup editPopup)
    {
        var isDm = lobby.IsModeratorOrAbove;
        var currentParticipant = encounter.Participants.FirstOrDefault(p => p.IsCurrent);
        var isMyTurn = currentParticipant != null && currentParticipant.PlayerId == lobby.PlayerId;

        var buttonSize = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var totalWidth = buttonSize * 4 + spacing * 3;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - totalWidth) * 0.5f);

        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_prev", FontAwesomeIcon.ChevronLeft))
            {
                // Previous turn
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm && !isMyTurn))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_next", FontAwesomeIcon.ChevronRight))
            {
                // Next turn
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_dice", FontAwesomeIcon.DiceD20))
            {
                // Roll for initiative
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton($"##{encounterId}_menu", FontAwesomeIcon.EllipsisV))
            {
                ImGui.OpenPopup($"EncounterMenu##{encounterId}");
            }

            DrawContextMenu(encounterId, encounter, lobby, editPopup);
        }
    }

    private void DrawContextMenu(string encounterId, EncounterState encounter, Lobby lobby, EncounterEditPopup editPopup)
    {
        using var popup = ImRaii.Popup($"EncounterMenu##{encounterId}");
        if (!popup.Success) return;

        if (ImGui.MenuItem("Edit Encounter"))
        {
            editPopup.OpenForEdit(lobby, encounter);
        }
    }

    private void DrawParticipantsTable(string encounterId, EncounterState encounter)
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table($"Participants##{encounterId}", 2, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Initiative", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        foreach (var participant in encounter.Participants)
        {
            DrawParticipantRow(encounterId, participant);
        }
    }

    private void DrawParticipantRow(string encounterId, EncounterParticipant participant)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        if (participant.IsCurrent)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TextColored(Theme.YellowColor, FontAwesomeIcon.Star.ToIconString());
            }
            ImGui.SameLine();
        }
        ImGui.Text(participant.DisplayName);

        ImGui.TableNextColumn();
        if (!_initiativeBuffers.ContainsKey(participant.ParticipantId))
            _initiativeBuffers[participant.ParticipantId] = participant.Initiative ?? 0;

        var value = _initiativeBuffers[participant.ParticipantId];
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputInt($"##Init{encounterId}_{participant.ParticipantId}", ref value, 0, 0))
        {
            _initiativeBuffers[participant.ParticipantId] = value;
            // TODO: Send initiative update to server
        }
    }
}
