using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RpUtils.UI;

namespace RpUtils.UI.Windows;

internal class LobbyWindow : Window
{
    public LobbyWindow() : base("Lobbies")
    {
        Flags = Theme.CompactWindowFlags;
        IsOpen = false;
    }

    public override void Draw()
    {
        ImGui.Text("Coming soon...");
    }
}
