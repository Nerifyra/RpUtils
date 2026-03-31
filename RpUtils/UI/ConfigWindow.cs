using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.UI.Config;
using System.Numerics;

namespace RpUtils.UI.Windows;

public class ConfigWindow : Window
{
    public ConfigWindow() : base("RpUtils Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 200),
            MaximumSize = new Vector2(400, 400),
        };
    }

    public override void Draw()
    {
        using var tabBar = ImRaii.TabBar("ConfigTabs");
        if (!tabBar.Success) return;

        GeneralConfigTab.Draw();
        LobbyConfigTab.Draw();
    }
}
