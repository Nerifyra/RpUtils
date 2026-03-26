using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Features.Encounters.UI;
using RpUtils.Features.Lobbies.Models;
using RpUtils.UI;

namespace RpUtils.Features.Lobbies.UI;

internal class LobbyDetailWindow : Window
{
    private readonly string _lobbyId;
    private readonly ManageTab _manageTab;
    private readonly EncountersTab _encountersTab;
    private string _renameBuffer = string.Empty;
    private bool _openRenamePopup;

    public LobbyDetailWindow(string lobbyId) : base($"Lobby##{lobbyId}")
    {
        _lobbyId = lobbyId;
        _manageTab = new ManageTab(lobbyId);
        _encountersTab = new EncountersTab(lobbyId);
        Flags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(300, 200),
        };
        IsOpen = true;
    }

    public override void Draw()
    {
        if (!Plugin.Lobbies.Lobbies.TryGetValue(_lobbyId, out var lobby))
        {
            ImGui.Text("Loading lobby...");
            return;
        }

        // Update title to show lobby name
        WindowName = $"{lobby.State.Name}##{_lobbyId}";

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
    }

    private void DrawHeader(Lobby lobby)
    {
        var joinCode = lobby.State.JoinCode;
        var windowWidth = ImGui.GetContentRegionAvail().X;
        bool isHovered;

        using (Plugin.UI.Fonts.Header.Push())
        {
            var textWidth = ImGui.CalcTextSize(joinCode).X;
            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
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
        if (popup.Success)
        {
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
