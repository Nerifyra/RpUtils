using Dalamud.Bindings.ImGui;
using RpUtils.Models;
using System.Numerics;

namespace RpUtils.UI.Components;

public class ConnectionStatusIndicator
{
    public void Draw()
    {
        var status = Plugin.ConnectionStatus.Status;
        var color = GetConnectionColor(status);
        ImGui.TextColored(color, $"{status}");
    }

    private static Vector4 GetConnectionColor(ConnectionState state) => state switch
    {
        ConnectionState.Connected => Theme.GreenColor,
        ConnectionState.Reconnecting => Theme.YellowColor,
        ConnectionState.Connecting => Theme.YellowColor,
        ConnectionState.Disconnected => Theme.RedColor,
        ConnectionState.Disabled => Theme.GrayColor,
        _ => Theme.WhiteColor,
    };
}
