using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace RpUtils.UI;

public static class Theme
{
    // ── Text Colors ─────────────────────────────────────────────────────
    public static readonly Vector4 GreenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 YellowColor = new(1.0f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 GoldColor = new(0.85f, 0.65f, 0.13f, 1.0f);
    public static readonly Vector4 RedColor = new(1.0f, 0.0f, 0.0f, 1.0f);
    public static readonly Vector4 PurpleColor = new(0.7f, 0.5f, 1.0f, 1.0f);
    public static readonly Vector4 GrayColor = new(0.5f, 0.5f, 0.5f, 1.0f);
    public static readonly Vector4 WhiteColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    // ── Chat SeString Colors (FFXIV UI foreground IDs) ──────────────────
    public const ushort ChatPrefixColor = 540;   // Soft gold
    public const ushort ChatHighlightColor = 34;  // Bright white
    public const ushort ChatGreenColor = 60;
    public const ushort ChatRedColor = 17;

    // ── Cell Background Tints ────────────────────────────────────────────
    public static readonly Vector4 PendingTint = new(0.8f, 0.6f, 0.0f, 0.3f);
    public static readonly Vector4 SuccessTint = new(0.0f, 0.6f, 0.0f, 0.3f);
    public static readonly Vector4 FailureTint = new(0.6f, 0.0f, 0.0f, 0.3f);
    public static readonly Vector4 NextRollTint = new(0.85f, 0.65f, 0.13f, 0.6f);
    public static readonly Vector4 InactiveTint = new(0.5f, 0.5f, 0.5f, 0.3f);
    public static readonly Vector4 TransparentTint = new(0.0f, 0.0f, 0.0f, 0.0f);

    // ── Window Flags ───────────────────────────────────────────────────
    public const ImGuiWindowFlags CompactWindowFlags =
        ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize |
        ImGuiWindowFlags.AlwaysAutoResize;

    /// <summary>
    /// Pushes the full RpUtils theme style. Dispose the returned handle to pop.
    /// </summary>
    public static ThemeScope PushStyle()
    {
        // Start minimal — add color/spacing overrides here as the design evolves.
        // Each push must be matched by a pop in Dispose.
        var colorCount = 0;
        var varCount = 0;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 4.0f);
        varCount++;

        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
        varCount++;

        return new ThemeScope(colorCount, varCount);
    }

    public readonly struct ThemeScope : IDisposable
    {
        private readonly int _colorCount;
        private readonly int _varCount;

        internal ThemeScope(int colorCount, int varCount)
        {
            _colorCount = colorCount;
            _varCount = varCount;
        }

        public void Dispose()
        {
            if (_colorCount > 0) ImGui.PopStyleColor(_colorCount);
            if (_varCount > 0) ImGui.PopStyleVar(_varCount);
        }
    }
}
