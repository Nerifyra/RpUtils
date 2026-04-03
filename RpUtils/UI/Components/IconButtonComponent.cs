using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using System;

namespace RpUtils.UI.Components;

public static class IconButtonComponent
{
    public static void Draw(FontAwesomeIcon icon, string tooltip, Action onLeftClick, Action? onRightClick = null)
    {
        ImGuiComponents.IconButton(icon);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            onLeftClick.Invoke();
        }

        if (onRightClick is not null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            onRightClick.Invoke();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
