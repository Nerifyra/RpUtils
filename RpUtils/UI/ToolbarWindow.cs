using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Models;
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
        var tooltip = sonar.IsSharingLocation
            ? "Sonar\nLeft click: Stop sharing location\nRight click: Open location sharing window"
            : "Sonar\nLeft click: Start sharing location\nRight click: Open location sharing window";

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
            sonar.ToggleSharing();
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _toggleShareLocationWindow.Invoke();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }

    public override bool DrawConditions()
    {
        return Plugin.ClientState.IsLoggedIn;
    }

    public override void Draw()
    {
        var isConnected = Plugin.ConnectionStatus.Status == ConnectionState.Connected;
        using (ImRaii.Disabled(!isConnected))
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
