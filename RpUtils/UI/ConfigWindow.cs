using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RpUtils.UI;
using System.Threading.Tasks;

namespace RpUtils.UI.Windows;

public class ConfigWindow : Window
{
    public ConfigWindow() : base("RpUtils Configuration")
    {
        Flags = Theme.CompactWindowFlags;
    }

    public override void Draw()
    {
        var config = Plugin.Configuration;
        var enableRpUtils = config.EnableRpUtils;
        if (ImGui.Checkbox("Enable RpUtils Connection", ref enableRpUtils))
        {
            config.EnableRpUtils = enableRpUtils;
            config.Save();
            Task.Run(async () =>
            {
                if (enableRpUtils)
                {
                    await Plugin.ConnectionStatus.ConnectAsync();
                }
                else
                {
                    await Plugin.ConnectionStatus.DisconnectAsync();
                }
            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggling off disables the connection to RpUtils server and all features.");
        }
    }
}
