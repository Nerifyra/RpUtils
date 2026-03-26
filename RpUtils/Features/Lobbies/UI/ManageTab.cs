using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Lobbies.Models;
using System.Collections.Generic;

namespace RpUtils.Features.Lobbies.UI;

internal class ManageTab
{
    private readonly string _lobbyId;
    private readonly HashSet<string> _showCharacterName = [];
    private string _displayNameBuffer = string.Empty;
    private string _displayNameTargetPlayerId = string.Empty;
    private bool _openDisplayNamePopup;
    private string _charNameBuffer = string.Empty;
    private string _charNameTargetPlayerId = string.Empty;
    private bool _openCharNamePopup;

    public ManageTab(string lobbyId)
    {
        _lobbyId = lobbyId;
    }

    public void Draw(Lobby lobby)
    {
        using var tab = ImRaii.TabItem($"Manage##{_lobbyId}");
        if (!tab.Success) return;

        if (_openDisplayNamePopup)
        {
            ImGui.OpenPopup($"DisplayNamePopup##{_lobbyId}");
            _openDisplayNamePopup = false;
        }

        if (_openCharNamePopup)
        {
            ImGui.OpenPopup($"CharNamePopup##{_lobbyId}");
            _openCharNamePopup = false;
        }

        DrawDisplayNamePopup();
        DrawCharNamePopup();

        using var child = ImRaii.Child($"ManageScroll##{_lobbyId}", new System.Numerics.Vector2(0, 0), false);
        if (!child.Success) return;

        DrawMembersTable(lobby);
    }

    private void DrawMembersTable(Lobby lobby)
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table($"Members##{_lobbyId}", 3, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Role", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableHeadersRow();

        foreach (var member in lobby.State.Members)
        {
            DrawMemberRow(lobby, member);
        }
    }

    private void DrawMemberRow(Lobby lobby, LobbyMember member)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        var showCharName = _showCharacterName.Contains(member.PlayerId);
        ImGui.Text(showCharName ? member.CharacterName : member.DisplayName);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            if (!_showCharacterName.Remove(member.PlayerId))
            {
                _showCharacterName.Add(member.PlayerId);
            }
        }

        ImGui.TableNextColumn();
        if (member.IsOwner || member.IsModerator)
        {
            var icon = member.IsOwner ? FontAwesomeIcon.Crown : FontAwesomeIcon.Shield;
            var tooltip = member.IsOwner ? "Owner" : "Moderator";

            ImGui.PushFont(UiBuilder.IconFont);
            var iconWidth = ImGui.CalcTextSize(icon.ToIconString()).X;
            var columnWidth = ImGui.GetColumnWidth();
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (columnWidth - iconWidth) * 0.5f);
            ImGui.TextDisabled(icon.ToIconString());
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        ImGui.TableNextColumn();
        var isSelf = member.PlayerId == lobby.PlayerId;
        var canManageOther = !isSelf && (
            lobby.IsOwner && !member.IsOwner ||
            lobby.IsModeratorOrAbove && !member.IsModeratorOrAbove
        );

        if (canManageOther || isSelf)
        {
            if (ImGuiComponents.IconButton($"##{member.PlayerId}_actions", FontAwesomeIcon.EllipsisV))
            {
                ImGui.OpenPopup($"MemberMenu##{member.PlayerId}");
            }

            DrawMemberContextMenu(lobby, member, canManageOther);
        }
    }

    private void DrawMemberContextMenu(Lobby lobby, LobbyMember member, bool canManageOther)
    {
        using var popup = ImRaii.Popup($"MemberMenu##{member.PlayerId}");
        if (!popup.Success) return;

        if (ImGui.MenuItem("Change Display Name"))
        {
            _displayNameBuffer = member.DisplayName;
            _displayNameTargetPlayerId = member.PlayerId;
            _openDisplayNamePopup = true;
        }

        if (lobby.IsModeratorOrAbove && ImGui.MenuItem("Change Character Name"))
        {
            _charNameBuffer = member.CharacterName;
            _charNameTargetPlayerId = member.PlayerId;
            _openCharNamePopup = true;
        }

        if (canManageOther)
        {
            if (lobby.IsOwner)
            {
                if (ImGui.MenuItem("Transfer Ownership"))
                {
                    Plugin.Lobbies.TransferOwnership(_lobbyId, member.PlayerId);
                }

                if (member.IsModerator && ImGui.MenuItem("Demote to Member"))
                {
                    Plugin.Lobbies.DemoteMember(_lobbyId, member.PlayerId);
                }

                if (!member.IsModerator && ImGui.MenuItem("Promote to Moderator"))
                {
                    Plugin.Lobbies.PromoteMember(_lobbyId, member.PlayerId);
                }
            }

            if (ImGui.MenuItem("Kick"))
            {
                Plugin.Lobbies.KickMember(_lobbyId, member.PlayerId);
            }
        }
    }

    private void DrawDisplayNamePopup()
    {
        using var popup = ImRaii.Popup($"DisplayNamePopup##{_lobbyId}");
        if (!popup.Success) return;

        ImGui.SetNextItemWidth(200);
        ImGui.SetKeyboardFocusHere();

        if (ImGui.InputText($"##DisplayName{_lobbyId}", ref _displayNameBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var newName = _displayNameBuffer.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                Plugin.Lobbies.UpdateMemberDisplayName(_lobbyId, _displayNameTargetPlayerId, newName);
            }

            ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }
    }

    private void DrawCharNamePopup()
    {
        using var popup = ImRaii.Popup($"CharNamePopup##{_lobbyId}");
        if (!popup.Success) return;

        ImGui.SetNextItemWidth(200);
        ImGui.SetKeyboardFocusHere();

        if (ImGui.InputText($"##CharName{_lobbyId}", ref _charNameBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var newName = _charNameBuffer.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                Plugin.Lobbies.UpdateMemberCharacterName(_lobbyId, _charNameTargetPlayerId, newName);
            }

            ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
