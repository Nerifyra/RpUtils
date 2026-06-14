using Dalamud.Bindings.ImGui;

namespace RpUtils.UI.Components;

public static class Layout
{
    public static void CenterCursorX(float widgetWidth)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        if (widgetWidth >= avail) return;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (avail - widgetWidth) * 0.5f);
    }
}
