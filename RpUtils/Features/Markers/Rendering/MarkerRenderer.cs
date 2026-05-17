using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Pictomancy;
using RpUtils.Features.Markers.Models;
using RpUtils.UI.Components;
using System;
using System.Numerics;

namespace RpUtils.Features.Markers.Rendering;

public sealed class MarkerRenderer : IDisposable
{
    private const float DotRadius = 0.1f;
    private const float DotBorderRadius = 0.13f;

    private const int RingSegments = 64;
    private const float RingRadius = 0.7f;

    private const float WaveArcHalfWidthRad = MathF.PI / 4f;
    private const float WaveArcThickness = 8f;
    private const float WaveArcBorderThickness = 12f;
    private const float OrbitSpeedRadPerSec = 1.4f;

    private const float IconHeight = 0.75f;
    private const uint IconTint = 0xFFFFFFFF;

    private const float StackPaddingPx = 4f;

    private const uint FillColor = 0xFFFFFFFF;
    private const uint BorderColor = 0xFF000000;

    private const float HiddenMarkerAlpha = 0.4f;

    private static readonly PctDrawHints DrawHints = new()
    {
        DefaultParams = new PctDxParams
        {
            OccludedAlpha = 0f,
            OcclusionTolerance = 0.05f,
        },
    };

    private float _alpha = 1f;

    public MarkerRenderer()
    {
        Plugin.PluginInterface.UiBuilder.Draw += Draw;
    }

    public void Dispose()
    {
        Plugin.PluginInterface.UiBuilder.Draw -= Draw;
    }

    private void Draw()
    {
        if (Plugin.Markers is null) return;
        if (!Plugin.ClientState.IsLoggedIn) return;

        var markers = Plugin.Markers.Markers;
        var placingMarker = Plugin.Markers.PlacingMarker;

        if (markers.Count == 0 && placingMarker is null) return;

        using var pictomancyScope = PctService.Draw(hints: DrawHints);
        if (pictomancyScope == null) return;

        var territoryType = Plugin.ClientState.TerritoryType;
        var waveCenterAngle = (float)(ImGui.GetTime() * OrbitSpeedRadPerSec) % MathF.Tau;

        foreach (var marker in markers.Values)
        {
            if (!ShouldDrawMarker(marker, territoryType)) continue;
            DrawMarker(marker, waveCenterAngle);
        }

        if (placingMarker is not null)
            DrawReticle(placingMarker, waveCenterAngle);
    }

    private static bool ShouldDrawMarker(Marker marker, uint currentTerritory)
    {
        if (!marker.IsPlaced) return false;
        if (marker.TerritoryType != currentTerritory) return false;

        if (marker.IsVisible) return true;
        return IsLobbyModerator(marker.LobbyId);
    }

    private static bool IsLobbyModerator(string lobbyId)
    {
        if (Plugin.Lobbies is null) return false;
        return Plugin.Lobbies.Lobbies.TryGetValue(lobbyId, out var lobby) && lobby.IsModeratorOrAbove;
    }

    private void DrawMarker(Marker marker, float waveCenterAngle)
    {
        _alpha = marker.IsVisible ? 1f : HiddenMarkerAlpha;

        DrawDot(marker.WorldPos);
        DrawRing(marker.WorldPos, waveCenterAngle, RingRadius * marker.Size);
        DrawOverlayStack(marker.WorldPos, marker.IconId, marker.Label, drawHiddenIndicator: !marker.IsVisible);
    }

    private void DrawReticle(Marker placingMarker, float waveCenterAngle)
    {
        _alpha = 1f;

        if (!Plugin.GameGui.ScreenToWorld(ImGui.GetMousePos(), out var worldPos)) return;
        DrawDot(worldPos);
        DrawRing(worldPos, waveCenterAngle, RingRadius * placingMarker.Size);
        DrawOverlayStack(worldPos, placingMarker.IconId, placingMarker.Label, drawHiddenIndicator: false);
    }

    private void DrawOverlayStack(Vector3 foot, uint iconId, string label, bool drawHiddenIndicator)
    {
        var iconAnchor = foot + new Vector3(0, IconHeight, 0);
        if (!Plugin.GameGui.WorldToScreen(iconAnchor, out var iconScreen)) return;

        var iconSize = Plugin.Configuration.MarkerIconSize;
        DrawIcon(iconScreen, iconId, iconSize);

        // Stack cursor moves upward as each element draws above the previous.
        var topY = iconScreen.Y - iconSize / 2f;
        DrawLabel(iconScreen.X, ref topY, label);
        if (drawHiddenIndicator) DrawHiddenIndicator(iconScreen.X, ref topY);
    }

    private void DrawDot(Vector3 pos)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddCircleFilled(pos, DotBorderRadius, Tint(BorderColor));
        drawList.AddCircleFilled(pos, DotRadius, Tint(FillColor));
    }

    private void DrawRing(Vector3 origin, float waveCenterAngle, float radius)
    {
        DrawWaveArc(origin, waveCenterAngle, radius);
        DrawWaveArc(origin, waveCenterAngle + MathF.PI, radius);
    }

    private void DrawWaveArc(Vector3 origin, float centerAngle, float radius)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddArc(origin, radius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(BorderColor), RingSegments, WaveArcBorderThickness);
        drawList.AddArc(origin, radius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(FillColor), RingSegments, WaveArcThickness);
    }

    private void DrawIcon(Vector2 screenCenter, uint iconId, float size)
    {
        IconDisplay.DrawOn(ImGui.GetBackgroundDrawList(), iconId, screenCenter, new Vector2(size, size), Tint(IconTint));
    }

    private void DrawLabel(float xCenter, ref float topY, string label)
    {
        if (string.IsNullOrEmpty(label)) return;

        var scale = Plugin.Configuration.MarkerLabelScale;
        var font = ImGui.GetFont();
        var fontSize = font.FontSize * scale;
        var textSize = ImGui.CalcTextSize(label) * scale;

        var pos = new Vector2(xCenter - textSize.X / 2f, topY - StackPaddingPx - textSize.Y);
        ImGui.GetBackgroundDrawList().AddText(font, fontSize, pos, Tint(FillColor), label);
        topY = pos.Y;
    }

    private static void DrawHiddenIndicator(float xCenter, ref float topY)
    {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        var glyph = FontAwesomeIcon.EyeSlash.ToIconString();
        var glyphSize = ImGui.CalcTextSize(glyph);
        var pos = new Vector2(xCenter - glyphSize.X / 2f, topY - StackPaddingPx - glyphSize.Y);
        ImGui.GetBackgroundDrawList().AddText(pos, FillColor, glyph);
        topY = pos.Y;
    }

    private uint Tint(uint color)
    {
        var alphaByte = (color >> 24) & 0xFF;
        var newAlpha = (uint)(alphaByte * _alpha);
        return (color & 0x00FFFFFF) | (newAlpha << 24);
    }
}
