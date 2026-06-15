using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace RpUtils.UI.Components;

public static class CenteredButton
{
    public static bool Draw(string label)
    {
        var width = ImGui.CalcTextSize(label).X + ImGui.GetStyle().FramePadding.X * 2;
        Layout.CenterCursorX(width);
        return ImGui.Button(label);
    }

    public static bool Draw(string label, float width)
    {
        Layout.CenterCursorX(width);
        return ImGui.Button(label, new Vector2(width, 0));
    }
}
