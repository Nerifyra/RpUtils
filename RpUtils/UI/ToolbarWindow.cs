using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Services;
using RpUtils.UI;
using RpUtils.UI.Components;
using System;
using System.Numerics;

namespace RpUtils.UI.Windows;

internal class ToolbarWindow : Window
{
    private readonly ConnectionStatusIndicator _connectionIndicator = new();
    private readonly ISharedImmediateTexture? _rpIcon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(61545));

    private readonly Action _toggleShareLocationWindow;
    private readonly Action _toggleFindRoleplayWindow;
    private readonly Action _toggleLobbiesWindow;
    private readonly Action _toggleConfigWindow;

    public ToolbarWindow(
        Action toggleShareLocationWindow,
        Action toggleFindRoleplayWindow,
        Action toggleLobbiesWindow,
        Action toggleConfigWindow
    ) : base("##RpUtilsToolbar")
    {
        Flags = Theme.CompactWindowFlags | ImGuiWindowFlags.NoTitleBar;

        IsOpen = Plugin.Configuration.ShowToolbar;

        _toggleShareLocationWindow = toggleShareLocationWindow;
        _toggleFindRoleplayWindow = toggleFindRoleplayWindow;
        _toggleLobbiesWindow = toggleLobbiesWindow;
        _toggleConfigWindow = toggleConfigWindow;
    }

    private void DrawSonarButton()
    {
        var sonar = Plugin.Sonar;
        var tiedToRoleplaying = Plugin.Configuration.LinkSonarSharingToRoleplayingStatus;

        var header = tiedToRoleplaying ? "Sonar (linked to /roleplaying status)" : "Sonar";
        var primaryLine = tiedToRoleplaying
            ? $"Left click: {(sonar.IsSharingLocation ? "Disable" : "Enable")} /roleplaying"
            : $"Left click: {(sonar.IsSharingLocation ? "Stop" : "Start")} sharing location";
        var tooltip = $"{header}\n{primaryLine}\nRight click: Open location sharing window";

        if (sonar.IsSharingLocation && _rpIcon != null && _rpIcon.TryGetWrap(out var texture, out _))
        {
            ImGui.ImageButton(texture.Handle, new Vector2(16, 15));
        }
        else
        {
            ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt);
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            if (tiedToRoleplaying) ChatCommand.Send("/roleplaying");
            else sonar.ToggleSharing();
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _toggleShareLocationWindow.Invoke();
        }

        Tooltip.OnHover(tooltip);
    }

    public override bool DrawConditions()
    {
        return Plugin.ClientState.IsLoggedIn;
    }

    public override void OnOpen() => PersistToolbarVisibility(true);
    public override void OnClose() => PersistToolbarVisibility(false);

    private static void PersistToolbarVisibility(bool visible)
    {
        if (Plugin.Configuration.ShowToolbar == visible) return;
        Plugin.Configuration.ShowToolbar = visible;
        Plugin.Configuration.Save();
    }

    public override void Draw()
    {
        using (ImRaii.Disabled(!Plugin.ConnectionStatus.IsConnected))
        {
            ImGui.Text("Rp Utils:");
            ImGui.SameLine();
            _connectionIndicator.Draw();

            DrawSonarButton();
            ImGui.SameLine();
            IconButtonComponent.Draw(FontAwesomeIcon.MapMarkedAlt, "Find Roleplay", _toggleFindRoleplayWindow);
            ImGui.SameLine();
            IconButtonComponent.Draw(FontAwesomeIcon.PeopleGroup, "Lobbies", _toggleLobbiesWindow);
        }
        ImGui.SameLine();
        IconButtonComponent.Draw(FontAwesomeIcon.Cog, "Settings", _toggleConfigWindow);
    }
}
