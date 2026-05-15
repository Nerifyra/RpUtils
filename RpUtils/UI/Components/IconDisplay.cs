using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using System.Numerics;

namespace RpUtils.UI.Components;

public static class IconDisplay
{
    public static void Draw(uint iconId, Vector2 size)
    {
        if (TryGetWrap(iconId, out var wrap))
            ImGui.Image(wrap.Handle, size);
        else
            ImGui.Dummy(size);
    }

    public static bool DrawButton(uint iconId, Vector2 size)
    {
        if (TryGetWrap(iconId, out var wrap))
            return ImGui.ImageButton(wrap.Handle, size);

        using (ImRaii.Disabled())
            ImGui.Button($"##missing_{iconId}", size + ImGui.GetStyle().FramePadding * 2);
        return false;
    }

    public static void DrawOn(ImDrawListPtr drawList, uint iconId, Vector2 center, Vector2 size, uint tint)
    {
        if (!TryGetWrap(iconId, out var wrap)) return;
        var half = size / 2;
        drawList.AddImage(wrap.Handle, center - half, center + half, Vector2.Zero, Vector2.One, tint);
    }

    private static bool TryGetWrap(uint iconId, out IDalamudTextureWrap wrap)
    {
        var tex = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(iconId));
        if (tex != null && tex.TryGetWrap(out IDalamudTextureWrap? w, out _) && w != null)
        {
            wrap = w;
            return true;
        }
        wrap = null!;
        return false;
    }
}
