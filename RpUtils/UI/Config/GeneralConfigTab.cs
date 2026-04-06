using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using System.Numerics;
using System.Threading.Tasks;

namespace RpUtils.UI.Config;

internal static class GeneralConfigTab
{
    public static void Draw()
    {
        using var tab = ImRaii.TabItem("General");
        if (!tab.Success) return;

        // Button row
        if (ImGui.Button("Changelog"))
        {
            Plugin.UI.ToggleChangelogWindow();
        }
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.345f, 0.396f, 0.949f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.445f, 0.496f, 1.0f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.245f, 0.296f, 0.849f, 1.0f));
        if (ImGui.Button("Discord"))
        {
            Util.OpenLink(PluginConstants.DiscordInviteUrl);
        }
        ImGui.PopStyleColor(3);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var config = Plugin.Configuration;

        var enableRpUtils = config.EnableRpUtils;
        if (ImGui.Checkbox("Enable RpUtils Connection", ref enableRpUtils))
        {
            config.EnableRpUtils = enableRpUtils;
            config.Save();
            Task.Run(async () =>
            {
                if (enableRpUtils)
                    await Plugin.ConnectionStatus.ConnectAsync();
                else
                    await Plugin.ConnectionStatus.DisconnectAsync();
            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggling off disables the connection to RpUtils server and all features.");
        }

        ImGui.Spacing();

        var showChangelog = config.ShowChangelogOnUpdate;
        if (ImGui.Checkbox("Show changelog on important updates", ref showChangelog))
        {
            config.ShowChangelogOnUpdate = showChangelog;
            config.Save();
        }
    }
}
