using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Lobbies.Models;
using RpUtils.Features.Markers.Models;
using RpUtils.UI.Components;
using RpUtils.UI.Components.IconPicker;
using System.Linq;
using System.Numerics;

namespace RpUtils.Features.Markers.UI;

internal class MarkersTab
{
    private static readonly Vector2 RowIconSize = new(24, 24);

    private readonly string _lobbyId;
    private readonly IconPickerPopup _iconPickerPopup = new();
    private readonly MarkerConfigPopup _markerConfigPopup = new();

    public MarkersTab(string lobbyId)
    {
        _lobbyId = lobbyId;
    }

    public void Draw(Lobby lobby)
    {
        using var tab = ImRaii.TabItem($"Markers##{_lobbyId}");
        if (!tab.Success) return;

        var canManage = lobby.IsModeratorOrAbove;

        if (canManage) DrawMarkersControls();
        DrawMarkersList(canManage);
        if (canManage)
        {
            _iconPickerPopup.Draw();
            _markerConfigPopup.Draw();
        }
    }

    private void DrawMarkersControls()
    {
        var buttonSize = ImGui.GetFrameHeight();
        var totalWidth = buttonSize;
        ImGui.SetCursorPosX((ImGui.GetContentRegionAvail().X - totalWidth) * 0.5f);

        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            _ = Plugin.Markers.AddMarker(_lobbyId, IconPickerComponent.DefaultIconId);
        TooltipOnHover("Add marker");
    }

    private void DrawMarkersList(bool canManage)
    {
        var markers = Plugin.Markers.Markers.Values
            .Where(m => m.LobbyId == _lobbyId && (canManage || m.IsVisible))
            .ToList();

        if (markers.Count == 0)
        {
            ImGui.TextDisabled("No markers yet.");
            return;
        }

        using var child = ImRaii.Child($"MarkersScroll##{_lobbyId}", new Vector2(0, 0), false);
        if (!child.Success) return;

        foreach (var marker in markers)
        {
            if (canManage) DrawMarkerRow(marker);
            else DrawReadOnlyMarkerRow(marker);
        }
    }

    private static void DrawReadOnlyMarkerRow(Marker marker)
    {
        using var rowId = ImRaii.PushId(marker.Id.ToString());

        IconDisplay.Draw(marker.IconId, RowIconSize);

        ImGui.SameLine();
        // Vertically center label text against the icon.
        var textOffsetY = (RowIconSize.Y - ImGui.GetTextLineHeight()) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + textOffsetY);
        if (string.IsNullOrEmpty(marker.Label))
            ImGui.TextDisabled("(unnamed)");
        else
            ImGui.TextUnformatted(marker.Label);
    }

    private void DrawMarkerRow(Marker marker)
    {
        using var rowId = ImRaii.PushId(marker.Id.ToString());

        if (IconDisplay.DrawButton(marker.IconId, RowIconSize))
            _iconPickerPopup.Open(marker.IconId, id =>
            {
                marker.IconId = id;
                _ = Plugin.Markers.UpdateMarker(marker);
            });
        TooltipOnHover("Change icon");

        ImGui.SameLine();

        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var btnWidth = ImGui.GetFrameHeight();
        var rightReserve = (btnWidth + spacing) * 5;

        var label = marker.Label;
        ImGui.SetNextItemWidth(-rightReserve);
        if (ImGui.InputTextWithHint("##Label", "Label", ref label, 64))
            marker.Label = label;
        if (ImGui.IsItemDeactivatedAfterEdit())
            _ = Plugin.Markers.UpdateMarker(marker);

        ImGui.SameLine();
        var visIcon = marker.IsVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
        if (ImGuiComponents.IconButton(visIcon))
        {
            marker.IsVisible = !marker.IsVisible;
            _ = Plugin.Markers.UpdateMarker(marker);
        }
        TooltipOnHover(marker.IsVisible ? "Hide marker" : "Show marker");

        ImGui.SameLine();
        var isPlacingThis = Plugin.Markers.PlacingMarker == marker;
        if (isPlacingThis)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Ban))
                Plugin.Markers.CancelPlacement();
            TooltipOnHover("Cancel placement (or right-click / Esc)");
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt))
                Plugin.Markers.BeginPlacement(marker);
            TooltipOnHover(marker.IsPlaced ? "Re-place with reticle" : "Place with reticle");
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            _markerConfigPopup.Open(marker);
        TooltipOnHover("Configure");

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            _ = Plugin.Markers.RemoveMarker(marker.Id);
        TooltipOnHover("Remove marker");
    }

    private static void TooltipOnHover(string text)
    {
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(text);
    }
}
