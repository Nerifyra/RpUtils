using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RpUtils.UI.Components.IconPicker;

public sealed class IconPickerComponent
{
    public const uint DefaultIconId = 60424;

    private const float GridHeight = 280f;
    private const float JumpComboWidth = 180f;

    private static readonly Vector2 PreviewSize = new(32, 32);
    private static readonly Vector2 GridIconSize = new(32, 32);
    private const uint SelectedColor = 0xFF1A85FF;
    private const uint SelectedHoverColor = 0xFF3A95FF;

    // Tried pulling these from Lumina sheets, I don't think they're all entirely accurate at the moment
    private static readonly (string Label, uint IconId)[] JumpTargets =
    [
        ("General actions",  101),
        ("Actions",          103),
        ("Achievements",     1001),
        ("Crafter actions",  1501),
        ("Mounts",           4001),
        ("Minions",          4401),
        ("Traits",           5185),
        ("Key items",        25001),
        ("Items",            54001),
        ("Map symbols",      60311),
        ("Online status",    61501),
        ("Duties",           80103),
        ("Status effects",   215001),
        ("Emotes",           246101),
    ];

    public uint SelectedIconId { get; set; } = DefaultIconId;

    public void Draw()
    {
        DrawPreview();
        ImGui.Spacing();
        var jumpTo = DrawJumpTo();
        DrawGrid(jumpTo);
    }

    private void DrawPreview()
    {
        IconDisplay.Draw(SelectedIconId, PreviewSize);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled($"#{SelectedIconId}");
        ImGui.SameLine();
        if (ImGui.SmallButton("Reset"))
            SelectedIconId = DefaultIconId;
    }

    private uint? DrawJumpTo()
    {
        using var disabled = ImRaii.Disabled(!GameIconIndex.IsReady);
        ImGui.SetNextItemWidth(JumpComboWidth);
        if (!ImGui.BeginCombo("##JumpTo", "Jump to…")) return null;

        uint? selected = null;
        foreach (var (label, iconId) in JumpTargets)
        {
            if (ImGui.Selectable($"{label}  ({iconId})"))
                selected = iconId;
        }
        ImGui.EndCombo();
        return selected;
    }

    private void DrawGrid(uint? jumpTo)
    {
        using var child = ImRaii.Child("##IconGrid", new Vector2(0, GridHeight), true);
        if (!child.Success) return;

        if (!GameIconIndex.IsReady) { ImGui.TextDisabled("Indexing icons…"); return; }

        var icons = GameIconIndex.Icons;
        if (icons.Count == 0) { ImGui.TextDisabled("No icons found."); return; }

        var (columns, rowHeight, rowCount) = ComputeGridLayout(icons.Count);
        ApplyJumpScroll(jumpTo, columns, rowHeight);
        DrawClippedGrid(icons, columns, rowHeight, rowCount);
    }

    private static (int columns, float rowHeight, int rowCount) ComputeGridLayout(int itemCount)
    {
        var style = ImGui.GetStyle();
        var cellWidth = GridIconSize.X + style.FramePadding.X * 2 + style.ItemSpacing.X;
        var rowHeight = GridIconSize.Y + style.FramePadding.Y * 2 + style.ItemSpacing.Y;
        var columns = Math.Max(1, (int)MathF.Floor(ImGui.GetContentRegionAvail().X / cellWidth));
        var rowCount = (itemCount + columns - 1) / columns;
        return (columns, rowHeight, rowCount);
    }

    private static void ApplyJumpScroll(uint? jumpTo, int columns, float rowHeight)
    {
        if (!jumpTo.HasValue) return;
        var idx = GameIconIndex.FindNearest(jumpTo.Value);
        if (idx.HasValue)
            ImGui.SetScrollY(idx.Value / columns * rowHeight);
    }

    private void DrawClippedGrid(IReadOnlyList<uint> icons, int columns, float rowHeight, int rowCount)
    {
        var clipper = new ImGuiListClipper();
        clipper.Begin(rowCount, rowHeight);
        while (clipper.Step())
        {
            for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var idx = row * columns + col;
                    if (idx >= icons.Count) break;
                    if (col > 0) ImGui.SameLine();
                    DrawGridIcon(icons[idx]);
                }
            }
        }
        clipper.End();
    }

    private void DrawGridIcon(uint iconId)
    {
        var selected = iconId == SelectedIconId;

        using var btnColor   = ImRaii.PushColor(ImGuiCol.Button,        SelectedColor,      selected);
        using var hoverColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, SelectedHoverColor, selected);
        using var id         = ImRaii.PushId((int)iconId);

        var clicked = IconDisplay.DrawButton(iconId, GridIconSize);

        if (clicked) SelectedIconId = iconId;
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(iconId.ToString());
    }
}
