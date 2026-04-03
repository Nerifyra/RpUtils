using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.Models;
using RpUtils.Features.Lobbies.Models;
using RpUtils.Features.Rolls.Models;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Theme = RpUtils.UI.Theme;

namespace RpUtils.Features.Encounters.UI;

internal class EncounterDetailTab
{
    private readonly Dictionary<string, int> _initiativeBuffers = [];
    private readonly HashSet<string> _activeInputs = [];
    private readonly RollConfigPopup _rollConfigPopup = new();
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
                _rollConfigPopup.Open(encounterId, encounter);
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
        _rollConfigPopup.Draw(encounter);
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
        var isDm = lobby.IsModeratorOrAbove;
        var rolls = Plugin.Rolls.GetRollsForEncounter(encounterId)
            .Where(r => !r.IsInitiativeRoll)
            .ToList();
        var columnCount = 3 + rolls.Count; // Icon, Name, Initiative, + one per roll

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table($"Participants##{encounterId}", columnCount, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("##Icon", ImGuiTableColumnFlags.WidthFixed, 20);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Initiative", ImGuiTableColumnFlags.WidthFixed, 60);

        foreach (var roll in rolls)
        {
            ImGui.TableSetupColumn($"##roll_{roll.RollRequestId}", ImGuiTableColumnFlags.WidthFixed, 80);
        }

        // Manual header row so we can customize roll column headers
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableSetColumnIndex(0);
        ImGui.TableHeader("##Icon");

        ImGui.TableSetColumnIndex(1);
        ImGui.TableHeader("Name");

        ImGui.TableSetColumnIndex(2);
        ImGui.TableHeader("Initiative");

        for (var i = 0; i < rolls.Count; i++)
        {
            ImGui.TableSetColumnIndex(3 + i);
            DrawRollColumnHeader(rolls[i], isDm);
        }

        // Determine which cells will receive the next roll from the local player.
        // Matches ChatRollListener logic: rolls are processed in creation order,
        // player matches are checked first, then NPCs in initiative order (DM only).
        var nextRollTargetIds = new HashSet<string>();
        var activeRolls = rolls.Where(r => r.IsActive).OrderBy(r => r.CreatedAtUtc).ToList();
        var selfHighlighted = false;

        foreach (var roll in activeRolls)
        {
            // Check if the local player is still pending in this roll
            var selfParticipant = roll.Participants
                .FirstOrDefault(p => p.IsPending && !p.IsNpc && p.PlayerId == lobby.PlayerId);

            if (selfParticipant != null && !selfHighlighted)
            {
                // Only highlight the first (oldest) roll — that's where the listener assigns
                nextRollTargetIds.Add($"{roll.RollRequestId}_{selfParticipant.ParticipantId}");
                selfHighlighted = true;
                continue;
            }

            // For DMs: if they've already rolled in this request, highlight the next pending NPC
            if (isDm && selfParticipant == null)
            {
                var pendingNpcIds = roll.Participants
                    .Where(p => p.IsPending && p.IsNpc)
                    .Select(p => p.ParticipantId)
                    .ToHashSet();

                if (pendingNpcIds.Count > 0)
                {
                    var firstNpcId = encounter.Participants
                        .Where(p => pendingNpcIds.Contains(p.ParticipantId))
                        .Select(p => p.ParticipantId)
                        .FirstOrDefault();

                    if (firstNpcId != null)
                        nextRollTargetIds.Add($"{roll.RollRequestId}_{firstNpcId}");
                }
            }
        }

        foreach (var participant in encounter.Participants)
        {
            DrawParticipantRow(encounterId, participant, isDm, rolls, nextRollTargetIds);
        }

        // Deferred rename popup (must be at the same ID stack level)
        DrawRenameNpcPopup(encounterId);
    }

    private static void DrawRollColumnHeader(RollRequestState roll, bool isDm)
    {
        var rollLabel = string.IsNullOrWhiteSpace(roll.Name) ? "Roll" : roll.Name;

        // Header cell background color based on roll status
        var allResolved = roll.Participants.All(p => !p.IsPending);
        var headerBgColor = !roll.IsActive ? Theme.InactiveTint
            : allResolved ? Theme.SuccessTint
            : Theme.PendingTint;
        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.GetColorU32(headerBgColor));

        // Header text with DC
        if (roll.DC.HasValue)
        {
            ImGui.Text(rollLabel);
            ImGui.SameLine();
            ImGui.TextDisabled($"({roll.DC.Value})");
        }
        else
        {
            ImGui.Text(rollLabel);
        }

        // Ellipsis menu button for DMs, right-aligned
        if (isDm)
        {
            var buttonSize = ImGui.GetFrameHeight();
            ImGui.SameLine(ImGui.GetColumnWidth() - buttonSize);
            if (ImGuiComponents.IconButton($"##roll_menu_{roll.RollRequestId}", FontAwesomeIcon.EllipsisV))
            {
                ImGui.OpenPopup($"RollMenu##{roll.RollRequestId}");
            }
        }

        using var popup = ImRaii.Popup($"RollMenu##{roll.RollRequestId}");
        if (!popup.Success) return;

        if (roll.IsActive)
        {
            if (ImGui.MenuItem("End Roll"))
            {
                Plugin.Rolls.EndRollRequest(roll.RollRequestId);
            }
        }

        if (ImGui.MenuItem("Close Roll"))
        {
            Plugin.Rolls.CloseRollRequest(roll.RollRequestId);
        }
    }

    private void DrawParticipantRow(string encounterId, EncounterParticipant participant, bool isDm, List<RollRequestState> rolls, HashSet<string> nextRollTargetIds)
    {
        ImGui.TableNextRow();

        // Icon column
        ImGui.TableNextColumn();
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            if (participant.IsCurrent)
            {
                ImGui.TextColored(Theme.GoldColor, FontAwesomeIcon.Star.ToIconString());
            }
            else if (participant.IsNpc)
            {
                ImGui.TextColored(Theme.GrayColor, FontAwesomeIcon.Robot.ToIconString());
            }
            else if (participant.Role == nameof(Lobbies.Models.LobbyRole.Owner))
            {
                ImGui.TextColored(Theme.GrayColor, FontAwesomeIcon.Crown.ToIconString());
            }
            else if (participant.Role == nameof(Lobbies.Models.LobbyRole.Moderator))
            {
                ImGui.TextColored(Theme.GrayColor, FontAwesomeIcon.Shield.ToIconString());
            }
            else if (participant.Role == nameof(Lobbies.Models.LobbyRole.Ghost))
            {
                ImGui.TextColored(Theme.GrayColor, FontAwesomeIcon.Ghost.ToIconString());
            }
            else
            {
                ImGui.TextColored(Theme.GrayColor, FontAwesomeIcon.User.ToIconString());
            }
        }

        // Name column
        ImGui.TableNextColumn();

        // Invisible selectable spanning the full cell for right-click target
        ImGui.Selectable($"##Row{participant.ParticipantId}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
        var rowContextTarget = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        ImGui.SameLine(0, 0);

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

        // Roll result columns
        foreach (var roll in rolls)
        {
            ImGui.TableNextColumn();
            var rollParticipant = roll.Participants.FirstOrDefault(p => p.ParticipantId == participant.ParticipantId);
            var isNextTarget = nextRollTargetIds.Contains($"{roll.RollRequestId}_{participant.ParticipantId}");
            RollResultCell.Draw(encounterId, roll.RollRequestId, rollParticipant, roll.DC, roll.IsActive, isDm, isNextTarget);
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
