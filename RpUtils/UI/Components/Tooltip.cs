using Dalamud.Bindings.ImGui;

namespace RpUtils.UI.Components;

public static class Tooltip
{
    public static void OnHover(string text)
    {
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }
}
