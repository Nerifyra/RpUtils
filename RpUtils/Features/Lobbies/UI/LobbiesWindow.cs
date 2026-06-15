using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.UI.Components;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.Features.Lobbies.UI;

internal class LobbiesWindow : Window
{
    private string _joinCode = string.Empty;
    private readonly LobbyDetailView _detailView = new();

    public LobbiesWindow() : base("Lobbies")
    {
        IsOpen = false;
    }

    public override void OnOpen()
    {
        Task.Run(async () => await Plugin.Lobbies.RefreshLobbies());
    }

    public override void Draw()
    {
        using var disabled = ImRaii.Disabled(!Plugin.ConnectionStatus.IsConnected);

        if (Plugin.Lobbies.IsLoading)
        {
            ImGui.Text("Loading...");
            return;
        }

        var lobby = Plugin.Lobbies.Lobbies.Values.FirstOrDefault();
        if (lobby is null)
        {
            DrawEntry();
            return;
        }

        _detailView.Draw(lobby);
    }

    private void DrawEntry()
    {
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetContentRegionAvail().Y / 4f);

        DrawCreateButton();
        DrawJoinSection();
    }

    private void DrawCreateButton()
    {
        if (CenteredButton.Draw("Create Lobby"))
        {
            Plugin.Lobbies.CreateLobby();
        }
    }

    private void DrawJoinSection()
    {
        var style = ImGui.GetStyle();
        const float inputWidth = 120f;
        var groupWidth = inputWidth + style.ItemSpacing.X + ImGui.CalcTextSize("Join").X + style.FramePadding.X * 2;
        Layout.CenterCursorX(groupWidth);

        ImGui.SetNextItemWidth(inputWidth);
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
}
