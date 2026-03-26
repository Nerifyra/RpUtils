using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Lobbies.Models;
using System.Linq;

namespace RpUtils.Features.Encounters.UI;

internal class EncountersTab
{
    private readonly string _lobbyId;
    private readonly EncounterEditPopup _encounterPopup;
    private readonly EncounterDetailTab _encounterDetailTab = new();

    public EncountersTab(string lobbyId)
    {
        _lobbyId = lobbyId;
        _encounterPopup = new EncounterEditPopup(lobbyId);
    }

    public void Draw(Lobby lobby)
    {
        using var tab = ImRaii.TabItem($"Encounters##{_lobbyId}");
        if (!tab.Success) return;

        var encounters = Plugin.Encounters.Encounters
            .Where(e => e.Value.LobbyId == _lobbyId)
            .ToList();

        if (encounters.Count == 0)
        {
            DrawNoEncounters(lobby);
        }
        else
        {
            DrawEncounterTabs(lobby, encounters);
        }

        _encounterPopup.Draw(lobby);
    }

    private void DrawNoEncounters(Lobby lobby)
    {
        if (!lobby.IsModeratorOrAbove) return;

        var buttonText = "Roll for initiative...";
        var buttonWidth = ImGui.CalcTextSize(buttonText).X + ImGui.GetStyle().FramePadding.X * 2;
        var availWidth = ImGui.GetContentRegionAvail().X;
        var availHeight = ImGui.GetContentRegionAvail().Y;

        ImGui.SetCursorPosX((availWidth - buttonWidth) * 0.5f);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + availHeight * 0.4f);

        if (ImGui.Button(buttonText))
        {
            _encounterPopup.Open(lobby);
        }
    }

    private void DrawEncounterTabs(Lobby lobby, System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Models.EncounterState>> encounters)
    {
        using var tabBar = ImRaii.TabBar($"EncounterTabs##{_lobbyId}");
        if (!tabBar.Success) return;

        foreach (var (encounterId, encounter) in encounters)
        {
            _encounterDetailTab.Draw(encounterId, encounter, lobby);
        }

        if (lobby.IsModeratorOrAbove)
        {
            if (ImGui.TabItemButton($"+##{_lobbyId}_add", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
            {
                _encounterPopup.Open(lobby);
            }
        }
    }
}
