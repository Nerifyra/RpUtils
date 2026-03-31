using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Threading.Tasks;

namespace RpUtils.UI.Config;

internal static class GeneralConfigTab
{
    public static void Draw()
    {
        using var tab = ImRaii.TabItem("General");
        if (!tab.Success) return;

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
    }
}
