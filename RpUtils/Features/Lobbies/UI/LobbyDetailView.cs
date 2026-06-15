using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.UI;
using RpUtils.Features.Lobbies.Models;
using RpUtils.Features.Markers.UI;
using RpUtils.UI;
using RpUtils.UI.Components;

namespace RpUtils.Features.Lobbies.UI;

internal class LobbyDetailView
{
    private string _lobbyId = string.Empty;
    private ManageTab _manageTab = null!;
    private EncountersTab _encountersTab = null!;
    private MarkersTab _markersTab = null!;
    private string _renameBuffer = string.Empty;
    private bool _openRenamePopup;

    public void Draw(Lobby lobby)
    {
        if (_lobbyId != lobby.LobbyId)
        {
            _lobbyId = lobby.LobbyId;
            _manageTab = new ManageTab(lobby.LobbyId);
            _encountersTab = new EncountersTab(lobby.LobbyId);
            _markersTab = new MarkersTab(lobby.LobbyId);
        }

        DrawHeader(lobby);

        if (_openRenamePopup)
        {
            ImGui.OpenPopup($"RenamePopup##{_lobbyId}");
            _openRenamePopup = false;
        }

        DrawRenamePopup();

        ImGui.Separator();

        using var tabBar = ImRaii.TabBar($"LobbyTabs##{_lobbyId}");
        if (!tabBar.Success) return;

        _manageTab.Draw(lobby);
        _encountersTab.Draw(lobby);
        _markersTab.Draw(lobby);
    }

    private void DrawHeader(Lobby lobby)
    {
        var joinCode = lobby.State.JoinCode;
        var windowWidth = ImGui.GetContentRegionAvail().X;
        bool isHovered;

        using (Plugin.UI.Fonts.Header.Push())
        {
            Layout.CenterCursorX(ImGui.CalcTextSize(joinCode).X);
            ImGui.TextColored(Theme.GreenColor, joinCode);
            isHovered = ImGui.IsItemHovered();

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                ImGui.SetClipboardText(joinCode);
            }
        }

        if (isHovered)
        {
            ImGui.SetTooltip("Click to copy");
        }

        ImGui.SameLine(windowWidth - ImGui.GetFrameHeight());
        if (ImGuiComponents.IconButton($"##LobbyMenu{_lobbyId}", FontAwesomeIcon.EllipsisV))
        {
            ImGui.OpenPopup($"LobbyContextMenu##{_lobbyId}");
        }

        DrawContextMenu(lobby);
    }

    private void DrawContextMenu(Lobby lobby)
    {
        using var popup = ImRaii.Popup($"LobbyContextMenu##{_lobbyId}");
        if (!popup.Success) return;

        if (ImGui.MenuItem("Copy Join Code"))
        {
            ImGui.SetClipboardText(lobby.State.JoinCode);
        }

        if (lobby.IsModeratorOrAbove && ImGui.MenuItem("Refresh Join Code"))
        {
            Plugin.Lobbies.RegenerateJoinCode(_lobbyId);
        }

        if (lobby.IsModeratorOrAbove && ImGui.MenuItem("Rename Lobby"))
        {
            _renameBuffer = lobby.State.Name;
            _openRenamePopup = true;
        }

        if (lobby.IsOwner)
        {
            if (ImGui.MenuItem("Close Lobby"))
            {
                Plugin.Lobbies.CloseLobby(_lobbyId);
            }
        }
        else
        {
            if (ImGui.MenuItem("Leave Lobby"))
            {
                Plugin.Lobbies.LeaveLobby(_lobbyId);
            }
        }
    }

    private void DrawRenamePopup()
    {
        using var popup = ImRaii.Popup($"RenamePopup##{_lobbyId}");
        if (!popup.Success) return;

        ImGui.SetNextItemWidth(200);
        ImGui.SetKeyboardFocusHere();

        if (ImGui.InputText($"##Rename{_lobbyId}", ref _renameBuffer, 64, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            var newName = _renameBuffer.Trim();
            if (!string.IsNullOrEmpty(newName))
            {
                Plugin.Lobbies.RenameLobby(_lobbyId, newName);
            }

            ImGui.CloseCurrentPopup();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            ImGui.CloseCurrentPopup();
        }
    }
}
