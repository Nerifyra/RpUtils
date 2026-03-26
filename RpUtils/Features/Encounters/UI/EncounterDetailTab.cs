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
    private readonly HashSet<string> _activeInputs = [];
    private string _npcNameBuffer = string.Empty;
    private bool _openNpcPopup;
    private string _renameNpcBuffer = string.Empty;
    private string? _pendingRenameNpcId;
    private string? _activeRenameNpcId;

    public void Draw(string encounterId, EncounterState encounter, Lobby lobby, EncounterEditPopup editPopup)
    {
        using var tab = ImRaii.TabItem($"{encounter.Name}##{encounterId}");
        if (!tab.Success) return;

        DrawControls(encounterId, encounter, lobby, editPopup);

        ImGui.Separator();

        DrawParticipantsTable(encounterId, encounter, lobby);
    }

    private void DrawControls(string encounterId, EncounterState encounter, Lobby lobby, EncounterEditPopup editPopup)
    {
        var isDm = lobby.IsModeratorOrAbove;
        var currentParticipant = encounter.Participants.FirstOrDefault(p => p.IsCurrent);
        var isMyTurn = currentParticipant != null && currentParticipant.PlayerId == lobby.PlayerId;

        var buttonSize = ImGui.GetFrameHeight();
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var totalWidth = buttonSize * 5 + spacing * 4;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - totalWidth) * 0.5f);

        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_add", FontAwesomeIcon.Plus))
            {
                _npcNameBuffer = string.Empty;
                _openNpcPopup = true;
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_prev", FontAwesomeIcon.ChevronLeft))
            {
                Plugin.Encounters.ReverseTurn(encounterId);
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_dice", FontAwesomeIcon.DiceD20))
            {
                // Roll for initiative
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm && !isMyTurn))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_next", FontAwesomeIcon.ChevronRight))
            {
                Plugin.Encounters.AdvanceTurn(encounterId);
            }
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(!isDm))
        {
            if (ImGuiComponents.IconButton($"##{encounterId}_menu", FontAwesomeIcon.EllipsisV))
            {
                ImGui.OpenPopup($"EncounterMenu##{encounterId}");
            }

            DrawContextMenu(encounterId, encounter, lobby, editPopup);
        }

        DrawNpcPopup(encounterId);
    }

    private void DrawNpcPopup(string encounterId)
    {
        if (_openNpcPopup)
        {
            ImGui.OpenPopup($"AddNpc##{encounterId}");
            _openNpcPopup = false;
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 0), ImGuiCond.Always);
        using var popup = ImRaii.Popup($"AddNpc##{encounterId}");
        if (!popup.Success) return;

        ImGui.Text("NPC Name");
        ImGui.SetNextItemWidth(-1);
        var submit = ImGui.InputText($"##NpcName{encounterId}", ref _npcNameBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue);

        if (submit || ImGui.Button("Add", new System.Numerics.Vector2(-1, 0)))
        {
            if (!string.IsNullOrWhiteSpace(_npcNameBuffer))
            {
                Plugin.Encounters.AddNpcParticipant(encounterId, _npcNameBuffer.Trim());
                _npcNameBuffer = string.Empty;
                ImGui.CloseCurrentPopup();
            }
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

        if (ImGui.MenuItem("End Encounter"))
        {
            Plugin.Encounters.EndEncounter(encounterId);
        }
    }

    private void DrawParticipantsTable(string encounterId, EncounterState encounter, Lobby lobby)
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table($"Participants##{encounterId}", 2, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Initiative", ImGuiTableColumnFlags.WidthFixed, 60);
        ImGui.TableHeadersRow();

        foreach (var participant in encounter.Participants)
        {
            DrawParticipantRow(encounterId, participant, lobby.IsModeratorOrAbove);
        }

        // Deferred rename popup (must be at the same ID stack level)
        DrawRenameNpcPopup(encounterId);
    }

    private void DrawParticipantRow(string encounterId, EncounterParticipant participant, bool isDm)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();

        // Invisible selectable spanning the full cell for right-click target
        ImGui.Selectable($"##Row{participant.ParticipantId}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
        var rowContextTarget = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        ImGui.SameLine(0, 0);

        if (participant.IsCurrent)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                ImGui.TextColored(Theme.YellowColor, FontAwesomeIcon.Star.ToIconString());
            }
            ImGui.SameLine();
        }
        ImGui.Text(participant.DisplayName);

        // NPC context menu on the name column
        if (isDm && participant.IsNpc)
        {
            if (rowContextTarget)
                ImGui.OpenPopup($"NpcMenu##{participant.ParticipantId}");

            using (var popup = ImRaii.Popup($"NpcMenu##{participant.ParticipantId}"))
            {
                if (popup.Success)
                {
                    if (ImGui.MenuItem("Rename NPC"))
                    {
                        _renameNpcBuffer = participant.DisplayName;
                        _pendingRenameNpcId = participant.ParticipantId;
                    }

                    if (ImGui.MenuItem("Remove NPC"))
                    {
                        Plugin.Encounters.RemoveNpcParticipant(encounterId, participant.ParticipantId);
                    }
                }
            }
        }

        ImGui.TableNextColumn();
        var serverValue = participant.Initiative ?? 0;

        // Initialize buffer, or sync from server when not actively editing
        if (!_activeInputs.Contains(participant.ParticipantId))
            _initiativeBuffers[participant.ParticipantId] = serverValue;

        var value = _initiativeBuffers[participant.ParticipantId];
        ImGui.SetNextItemWidth(-1);
        using (ImRaii.Disabled(!isDm))
        {
            if (ImGui.InputInt($"##Init{encounterId}_{participant.ParticipantId}", ref value, 0, 0))
            {
                _initiativeBuffers[participant.ParticipantId] = value;
            }

            // Track whether this input is actively being edited
            if (ImGui.IsItemActive())
                _activeInputs.Add(participant.ParticipantId);

            // Send to server when the input loses focus, if the value changed
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _activeInputs.Remove(participant.ParticipantId);
                if (_initiativeBuffers[participant.ParticipantId] != serverValue)
                {
                    Plugin.Encounters.SetInitiative(encounterId, participant.ParticipantId, _initiativeBuffers[participant.ParticipantId]);
                }
            }
            else if (!ImGui.IsItemActive())
            {
                _activeInputs.Remove(participant.ParticipantId);
            }
        }
    }

    private void DrawRenameNpcPopup(string encounterId)
    {
        if (_pendingRenameNpcId != null)
        {
            _activeRenameNpcId = _pendingRenameNpcId;
            _pendingRenameNpcId = null;
            ImGui.OpenPopup($"RenameNpc##{encounterId}");
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, 0), ImGuiCond.Always);
        using var popup = ImRaii.Popup($"RenameNpc##{encounterId}");
        if (!popup.Success) return;

        ImGui.Text("New Name");
        ImGui.SetNextItemWidth(-1);
        var submit = ImGui.InputText($"##RenameNpcInput{encounterId}", ref _renameNpcBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue);

        if (submit || ImGui.Button("Rename", new System.Numerics.Vector2(-1, 0)))
        {
            if (!string.IsNullOrWhiteSpace(_renameNpcBuffer) && _activeRenameNpcId != null)
            {
                Plugin.Encounters.RenameNpcParticipant(encounterId, _activeRenameNpcId, _renameNpcBuffer.Trim());
                _activeRenameNpcId = null;
                ImGui.CloseCurrentPopup();
            }
        }
    }
}
