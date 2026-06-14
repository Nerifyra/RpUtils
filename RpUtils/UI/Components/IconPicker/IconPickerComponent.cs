using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RpUtils.UI.Components.IconPicker;

public sealed class IconPickerComponent
{
    public const uint DefaultIconId = 60424;

    private const float GridHeight = 280f;
    private const float JumpComboWidth = 180f;
    private const float JumpInputWidth = 90f;
    private const int FavoritesVisibleRows = 3;

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

    private int _jumpToInput;

    public void Draw()
    {
        DrawPreview();
        ImGui.Spacing();
        DrawFavorites();
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
        DrawFavoriteToggle();

        ImGui.SameLine();
        if (ImGui.SmallButton("Reset"))
            SelectedIconId = DefaultIconId;
    }

    private void DrawFavoriteToggle()
    {
        var isFavorite = IconFavorites.IsFavorite(SelectedIconId);
        using (ImRaii.PushColor(ImGuiCol.Text, isFavorite ? Theme.GoldColor : Theme.GrayColor))
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Star))
                IconFavorites.Toggle(SelectedIconId);
        }
        Tooltip.OnHover(isFavorite ? "Remove from favorites" : "Add to favorites");
    }

    private void DrawFavorites()
    {
        if (!ImGui.CollapsingHeader($"Favorites ({IconFavorites.Count})###IconFavorites"))
            return;

        if (IconFavorites.Count == 0)
        {
            ImGui.TextDisabled("Star an icon to add it here.");
            return;
        }

        var style = ImGui.GetStyle();
        var childHeight = RowHeight() * FavoritesVisibleRows + style.WindowPadding.Y * 2;
        using var child = ImRaii.Child("##Favorites", new Vector2(0, childHeight), true);
        if (!child.Success) return;

        var favorites = IconFavorites.All.ToArray();
        var columns = ComputeColumns();
        var rowCount = (favorites.Length + columns - 1) / columns;
        for (var row = 0; row < rowCount; row++)
            DrawIconRow(favorites, row * columns, columns);
    }

    private uint? DrawJumpTo()
    {
        using var disabled = ImRaii.Disabled(!GameIconIndex.IsReady);

        uint? selected = null;

        ImGui.SetNextItemWidth(JumpComboWidth);
        if (ImGui.BeginCombo("##JumpTo", "Jump to…"))
        {
            foreach (var (label, iconId) in JumpTargets)
            {
                if (ImGui.Selectable($"{label}  ({iconId})"))
                    selected = iconId;
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(JumpInputWidth);
        ImGui.InputInt("##JumpToInput", ref _jumpToInput, 0, 0);
        if (ImGui.IsItemDeactivatedAfterEdit())
        {
            if (_jumpToInput < 0) _jumpToInput = 0;
            selected = (uint)_jumpToInput;
        }
        Tooltip.OnHover("Type an icon # and press Enter to jump");

        return selected;
    }

    private void DrawGrid(uint? jumpTo)
    {
        using var child = ImRaii.Child("##IconGrid", new Vector2(0, GridHeight), true);
        if (!child.Success) return;

        if (!GameIconIndex.IsReady) { ImGui.TextDisabled("Indexing icons…"); return; }

        var icons = GameIconIndex.Icons;
        if (icons.Count == 0) { ImGui.TextDisabled("No icons found."); return; }

        var columns = ComputeColumns();
        var rowHeight = RowHeight();
        var rowCount = (icons.Count + columns - 1) / columns;
        ApplyJumpScroll(jumpTo, columns, rowHeight);
        DrawClippedGrid(icons, columns, rowHeight, rowCount);
    }

    private static int ComputeColumns()
    {
        var style = ImGui.GetStyle();
        var cellWidth = GridIconSize.X + style.FramePadding.X * 2 + style.ItemSpacing.X;
        return Math.Max(1, (int)MathF.Floor(ImGui.GetContentRegionAvail().X / cellWidth));
    }

    private static float RowHeight()
    {
        var style = ImGui.GetStyle();
        return GridIconSize.Y + style.FramePadding.Y * 2 + style.ItemSpacing.Y;
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
            for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                DrawIconRow(icons, row * columns, columns);
        clipper.End();
    }

    private void DrawIconRow(IReadOnlyList<uint> icons, int rowStart, int columns)
    {
        for (var col = 0; col < columns; col++)
        {
            var idx = rowStart + col;
            if (idx >= icons.Count) break;
            if (col > 0) ImGui.SameLine();
            DrawIconCell(icons[idx]);
        }
    }

    private void DrawIconCell(uint iconId)
    {
        var selected = iconId == SelectedIconId;

        using var btnColor   = ImRaii.PushColor(ImGuiCol.Button,        SelectedColor,      selected);
        using var hoverColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, SelectedHoverColor, selected);
        using var id         = ImRaii.PushId((int)iconId);

        if (IconDisplay.DrawButton(iconId, GridIconSize))
            SelectedIconId = iconId;

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            IconFavorites.Toggle(iconId);

        DrawCellTooltip(iconId);
    }

    private static void DrawCellTooltip(uint iconId)
    {
        if (!ImGui.IsItemHovered()) return;
        var verb = IconFavorites.IsFavorite(iconId) ? "unfavorite" : "favorite";
        ImGui.SetTooltip($"#{iconId}\nRight-click to {verb}");
    }
}
