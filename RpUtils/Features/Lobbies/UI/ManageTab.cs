using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Lobbies.Models;
using System.Collections.Generic;
using Theme = RpUtils.UI.Theme;

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
    private string _ghostDisplayNameBuffer = string.Empty;
    private string _ghostCharNameBuffer = string.Empty;
    private bool _openGhostPopup;

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

        if (_openGhostPopup)
        {
            ImGui.OpenPopup($"GhostPopup##{_lobbyId}");
            _openGhostPopup = false;
        }

        DrawDisplayNamePopup();
        DrawCharNamePopup();
        DrawGhostPopup();

        using var child = ImRaii.Child($"ManageScroll##{_lobbyId}", new System.Numerics.Vector2(0, 0), false);
        if (!child.Success) return;

        DrawMembersTable(lobby);
    }

    private void DrawMembersTable(Lobby lobby)
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table($"Members##{_lobbyId}", 3, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("##Icon", ImGuiTableColumnFlags.WidthFixed, 26);
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##Actions", ImGuiTableColumnFlags.WidthFixed, 30);

        // Custom header row
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);

        ImGui.TableNextColumn();
        if (lobby.IsModeratorOrAbove)
        {
            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                if (ImGui.SmallButton(FontAwesomeIcon.UserPlus.ToIconString() + $"##AddGhost{_lobbyId}"))
                {
                    // Auto-populate from current target if it's a player character
                    var targetInfo = Plugin.Lobbies.GetTargetPlayerInfo();
                    _ghostDisplayNameBuffer = targetInfo?.DisplayName ?? string.Empty;
                    _ghostCharNameBuffer = targetInfo?.CharacterName ?? string.Empty;
                    _openGhostPopup = true;
                }
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add Ghost Player");
        }

        ImGui.TableNextColumn();
        ImGui.TableHeader("Name");

        ImGui.TableNextColumn();

        foreach (var member in lobby.State.Members)
        {
            DrawMemberRow(lobby, member);
        }
    }

    private void DrawMemberRow(Lobby lobby, LobbyMember member)
    {
        ImGui.TableNextRow();

        // Icon column
        ImGui.TableNextColumn();
        {
            FontAwesomeIcon icon;
            System.Numerics.Vector4 color;
            string tooltip;

            if (member.IsOwner)
            {
                icon = FontAwesomeIcon.Crown;
                color = Theme.GrayColor;
                tooltip = "Owner";
            }
            else if (member.IsModerator)
            {
                icon = FontAwesomeIcon.Shield;
                color = Theme.GrayColor;
                tooltip = "Moderator";
            }
            else if (member.IsGhost)
            {
                icon = FontAwesomeIcon.Ghost;
                color = Theme.GrayColor;
                tooltip = "Ghost Player";
            }
            else
            {
                icon = FontAwesomeIcon.User;
                color = Theme.GrayColor;
                tooltip = "Member";
            }

            using (ImRaii.PushFont(UiBuilder.IconFont))
            {
                var iconText = icon.ToIconString();
                var iconWidth = ImGui.CalcTextSize(iconText).X;
                var columnWidth = ImGui.GetColumnWidth();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (columnWidth - iconWidth) * 0.5f);
                ImGui.TextColored(color, iconText);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        // Name column
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

        // Actions column
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
            if (member.IsGhost)
            {
                if (ImGui.MenuItem("Remove Ghost"))
                {
                    Plugin.Lobbies.RemoveGhostPlayer(_lobbyId, member.PlayerId);
                }
            }
            else
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

    private void DrawGhostPopup()
    {
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(300, 0), ImGuiCond.Always);
        using var popup = ImRaii.Popup($"GhostPopup##{_lobbyId}");
        if (!popup.Success) return;

        ImGui.PushTextWrapPos(0);
        ImGui.TextColored(Theme.GrayColor, "Ghost Players are used to represent non plugin users. Character Name should match the in-game name to properly track rolls.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        ImGui.Text("Display Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##GhostDisplayName{_lobbyId}", ref _ghostDisplayNameBuffer, 64);

        ImGui.Spacing();

        ImGui.Text("Character Name");
        ImGui.SetNextItemWidth(-1);
        ImGui.InputText($"##GhostCharName{_lobbyId}", ref _ghostCharNameBuffer, 64);

        ImGui.Spacing();

        var displayName = _ghostDisplayNameBuffer.Trim();
        var charName = _ghostCharNameBuffer.Trim();
        var canCreate = !string.IsNullOrEmpty(displayName) && !string.IsNullOrEmpty(charName);

        using (ImRaii.Disabled(!canCreate))
        {
            if (ImGui.Button("Create", new System.Numerics.Vector2(-1, 0)))
            {
                Plugin.Lobbies.CreateGhostPlayer(_lobbyId, displayName, charName);
                ImGui.CloseCurrentPopup();
            }
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
