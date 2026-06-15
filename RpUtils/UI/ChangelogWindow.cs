using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.UI.Components;
using System.Numerics;

namespace RpUtils.UI.Windows;

public class ChangelogWindow : Window
{
    public ChangelogWindow() : base("RpUtils - Changelog")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize;
        Size = new Vector2(600, 500);
    }

    public override void Draw()
    {
        // Scrollable changelog content
        var footerHeight = ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        using (var child = ImRaii.Child("ChangelogContent", new Vector2(0, -footerHeight), false))
        {
            if (child.Success)
            {
                Changelog.Instance.Draw();
            }
        }

        ImGui.Separator();

        // Show on update checkbox
        var config = Plugin.Configuration;
        var showOnUpdate = config.ShowChangelogOnUpdate;
        if (ImGui.Checkbox("Show changelog on updates", ref showOnUpdate))
        {
            config.ShowChangelogOnUpdate = showOnUpdate;
            config.Save();
        }

        if (CenteredButton.Draw("Close", 120f))
        {
            IsOpen = false;
        }
    }

    public override void OnClose()
    {
        var config = Plugin.Configuration;
        config.LastSeenChangelogVersion = PluginConstants.GetReleaseVersion(PluginConstants.PluginVersion);
        config.Save();
    }
}
