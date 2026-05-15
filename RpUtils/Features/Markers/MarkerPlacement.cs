using Dalamud.Bindings.ImGui;
using RpUtils.Features.Markers.Models;
using System;
using System.Runtime.InteropServices;

namespace RpUtils.Features.Markers;

internal sealed class MarkerPlacement : IDisposable
{
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;

    private bool _wasPlacing;
    private bool _prevLeftDown;
    private bool _prevRightDown;
    private bool _prevEscDown;

    public MarkerPlacement()
    {
        Plugin.PluginInterface.UiBuilder.Draw += OnFrame;
    }

    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.Draw -= OnFrame;
    }

    private void OnFrame()
    {
        if (Plugin.Markers is null) return;

        var placingMarker = Plugin.Markers.PlacingMarker;
        if (placingMarker is null)
        {
            _wasPlacing = false;
            return;
        }

        // KeyState for mouse clicks didn't seem to work. Maybe I did something wrong?
        var leftDown = IsKeyDown(VK_LBUTTON);
        var rightDown = IsKeyDown(VK_RBUTTON);
        var escDown = ImGui.IsKeyDown(ImGuiKey.Escape);

        // First frame of placement: swallow the UI click that triggered it.
        if (!_wasPlacing)
        {
            _prevLeftDown = leftDown;
            _prevRightDown = rightDown;
            _prevEscDown = escDown;
            _wasPlacing = true;
            return;
        }

        if (leftDown && !_prevLeftDown) Commit(placingMarker);
        else if ((rightDown && !_prevRightDown) || (escDown && !_prevEscDown))
            Plugin.Markers.CancelPlacement();

        _prevLeftDown = leftDown;
        _prevRightDown = rightDown;
        _prevEscDown = escDown;
    }

    private static void Commit(Marker marker)
    {
        if (!Plugin.GameGui.ScreenToWorld(ImGui.GetMousePos(), out var worldPos)) return;
        marker.WorldPos = worldPos;
        marker.TerritoryType = Plugin.ClientState.TerritoryType;
        marker.IsPlaced = true;
        _ = Plugin.Markers.UpdateMarker(marker);
        Plugin.Markers.CancelPlacement();
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool IsKeyDown(int vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;
}
