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

        if (canManage)
        {
            DrawMarkersControls();
            _iconPickerPopup.Draw();
            _markerConfigPopup.Draw();
        }
        DrawMarkersList(canManage);
    }

    private void DrawMarkersControls()
    {
        Layout.CenterCursorX(ImGui.GetFrameHeight());
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Plus))
            _ = Plugin.Markers.AddMarker(_lobbyId, IconPickerComponent.DefaultIconId);
        Tooltip.OnHover("Add marker");
    }

    private void DrawMarkersList(bool canManage)
    {
        // NPC-attached markers are edited from the encounter row (cog button there); hiding
        // them here keeps this tab focused on standalone markers
        var markers = Plugin.Markers.Markers.Values
            .Where(m => m.LobbyId == _lobbyId
                && (canManage || m.IsVisible)
                && m.NpcParticipantId == null)
            .ToList();

        if (markers.Count == 0)
        {
            ImGui.TextDisabled("No markers yet.");
            return;
        }

        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY;
        using var table = ImRaii.Table($"Markers##{_lobbyId}", 3, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("##icon", ImGuiTableColumnFlags.WidthFixed);
        ImGui.TableSetupColumn("##label", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("##actions", ImGuiTableColumnFlags.WidthFixed);

        foreach (var marker in markers)
        {
            using var rowId = ImRaii.PushId(marker.Id.ToString());
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            if (canManage) DrawMarkerIcon(marker);
            else IconDisplay.Draw(marker.IconId, RowIconSize);

            ImGui.TableNextColumn();
            if (canManage) DrawMarkerLabel(marker);
            else DrawReadOnlyLabel(marker);

            ImGui.TableNextColumn();
            if (canManage) DrawMarkerControls(marker);
        }
    }

    private static void DrawReadOnlyLabel(Marker marker)
    {
        var textOffsetY = (RowIconSize.Y - ImGui.GetTextLineHeight()) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + textOffsetY);
        if (string.IsNullOrEmpty(marker.Label))
            ImGui.TextDisabled("(unnamed)");
        else
            ImGui.TextUnformatted(marker.Label);
    }

    private void DrawMarkerIcon(Marker marker)
    {
        if (IconDisplay.DrawButton(marker.IconId, RowIconSize))
            _iconPickerPopup.Open(marker.IconId, id =>
            {
                marker.IconId = id;
                _ = Plugin.Markers.UpdateMarker(marker);
            });
        Tooltip.OnHover("Change icon");
    }

    private void DrawMarkerLabel(Marker marker)
    {
        var label = marker.Label;
        ImGui.SetNextItemWidth(-1); // fill the stretch column
        if (ImGui.InputTextWithHint("##Label", "Label", ref label, 64))
            marker.Label = label;
        if (ImGui.IsItemDeactivatedAfterEdit())
            _ = Plugin.Markers.UpdateMarker(marker);
    }

    private void DrawMarkerControls(Marker marker)
    {
        var visIcon = marker.IsVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
        if (ImGuiComponents.IconButton(visIcon))
        {
            marker.IsVisible = !marker.IsVisible;
            _ = Plugin.Markers.UpdateMarker(marker);
        }
        Tooltip.OnHover(marker.IsVisible ? "Hide marker" : "Show marker");

        ImGui.SameLine(0, 0);
        var isPlacingMarker = Plugin.Markers.PlacingMarker == marker;
        if (isPlacingMarker)
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Ban))
                Plugin.Markers.CancelPlacement();
            Tooltip.OnHover("Cancel placement (or right-click / Esc)");
        }
        else
        {
            if (ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt))
                Plugin.Markers.BeginPlacement(marker);
            Tooltip.OnHover(marker.IsPlaced ? "Re-place with reticle" : "Place with reticle");
        }

        ImGui.SameLine(0, 0);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Cog))
            _markerConfigPopup.Open(marker);
        Tooltip.OnHover("Configure");

        ImGui.SameLine(0, 0);
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Trash))
            _ = Plugin.Markers.RemoveMarker(marker.Id);
        Tooltip.OnHover("Remove marker");
    }
}
