using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Features.Lobbies.Models;
using RpUtils.UI;
using System.Threading.Tasks;

namespace RpUtils.Features.Lobbies.UI;

internal class LobbiesWindow : Window
{
    private string _joinCode = string.Empty;

    public LobbiesWindow() : base("Lobbies")
    {
        //Flags = Theme.CompactWindowFlags;
        IsOpen = false;
    }

    public override void OnOpen()
    {
        Task.Run(async () => await Plugin.Lobbies.RefreshLobbies());
    }

    public override void Draw()
    {
        using var disabled = ImRaii.Disabled(!Plugin.ConnectionStatus.IsConnected);

        DrawCreateButton();
        DrawJoinSection();

        ImGui.Separator();

        DrawLobbyList();
    }

    private void DrawCreateButton()
    {
        if (ImGui.Button("Create Lobby"))
        {
            Plugin.Lobbies.CreateLobby();
        }
    }

    private void DrawJoinSection()
    {
        ImGui.SetNextItemWidth(120);
        ImGui.InputTextWithHint("##JoinCode", "Enter code...", ref _joinCode, 6);
        ImGui.SameLine();
        using var joinDisabled = ImRaii.Disabled(string.IsNullOrWhiteSpace(_joinCode));
        if (ImGui.Button("Join"))
        {
            var code = _joinCode.Trim();
            _joinCode = string.Empty;
            Plugin.Lobbies.JoinLobby(code);
        }
    }

    private void DrawLobbyList()
    {
        var lobbies = Plugin.Lobbies;

        if (lobbies.IsLoading && lobbies.Lobbies.Count == 0)
        {
            ImGui.Text("Loading...");
            return;
        }

        if (lobbies.Lobbies.Count == 0)
        {
            ImGui.Text("No lobbies. Create or join one!");
            return;
        }

        foreach (var lobby in lobbies.Lobbies.Values)
        {
            DrawLobbyItem(lobby);
        }
    }

    private void DrawLobbyItem(Lobby lobby)
    {
        var label = $"{lobby.State.Name} ({lobby.State.Members.Count})";
        if (ImGui.Selectable($"{label}##{lobby.LobbyId}"))
        {
            Plugin.UI.OpenLobbyDetail(lobby.LobbyId);
        }

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight());
        var tooltip = lobby.IsOwner ? "Close Lobby" : "Leave Lobby";
        if (ImGuiComponents.IconButton($"##{lobby.LobbyId}_exit", FontAwesomeIcon.Times))
        {
            if (lobby.IsOwner)
                Plugin.Lobbies.CloseLobby(lobby.LobbyId);
            else
                Plugin.Lobbies.LeaveLobby(lobby.LobbyId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
