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
    private const float LabelOffsetAboveIcon = 0.3f;
    private const float HiddenIndicatorOffsetAboveLabel = 0.3f;
    private static readonly Vector2 IconSize = new(28, 28);
    private const uint IconTint = 0xFFFFFFFF;

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
        DrawRing(marker.WorldPos, waveCenterAngle);
        DrawIcon(marker.WorldPos, marker.IconId);
        DrawLabel(marker.WorldPos, marker.Label);

        if (!marker.IsVisible) DrawHiddenIndicator(marker.WorldPos);
    }

    private void DrawReticle(Marker placingMarker, float waveCenterAngle)
    {
        _alpha = 1f;

        if (!Plugin.GameGui.ScreenToWorld(ImGui.GetMousePos(), out var worldPos)) return;
        DrawDot(worldPos);
        DrawRing(worldPos, waveCenterAngle);
        DrawIcon(worldPos, placingMarker.IconId);
        DrawLabel(worldPos, placingMarker.Label);
    }

    private void DrawDot(Vector3 pos)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddCircleFilled(pos, DotBorderRadius, Tint(BorderColor));
        drawList.AddCircleFilled(pos, DotRadius, Tint(FillColor));
    }

    private void DrawRing(Vector3 origin, float waveCenterAngle)
    {
        DrawWaveArc(origin, waveCenterAngle);
        DrawWaveArc(origin, waveCenterAngle + MathF.PI);
    }

    private void DrawWaveArc(Vector3 origin, float centerAngle)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddArc(origin, RingRadius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(BorderColor), RingSegments, WaveArcBorderThickness);
        drawList.AddArc(origin, RingRadius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(FillColor), RingSegments, WaveArcThickness);
    }

    private void DrawIcon(Vector3 foot, uint iconId)
    {
        var iconWorldPos = foot + new Vector3(0, IconHeight, 0);
        if (!Plugin.GameGui.WorldToScreen(iconWorldPos, out var screenPos)) return;
        IconDisplay.DrawOn(ImGui.GetBackgroundDrawList(), iconId, screenPos, IconSize, Tint(IconTint));
    }

    private void DrawLabel(Vector3 foot, string label)
    {
        if (string.IsNullOrEmpty(label)) return;

        var drawList = PctService.GetDrawList();
        var labelPos = foot + new Vector3(0, IconHeight + LabelOffsetAboveIcon, 0);
        drawList.AddText(labelPos, Tint(FillColor), label);
    }

    private void DrawHiddenIndicator(Vector3 foot)
    {
        var worldPos = foot + new Vector3(0, IconHeight + LabelOffsetAboveIcon + HiddenIndicatorOffsetAboveLabel, 0);
        if (!Plugin.GameGui.WorldToScreen(worldPos, out var screenPos)) return;

        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        var glyph = FontAwesomeIcon.EyeSlash.ToIconString();
        var glyphSize = ImGui.CalcTextSize(glyph);
        ImGui.GetBackgroundDrawList().AddText(screenPos - glyphSize / 2, FillColor, glyph);
    }

    private uint Tint(uint color)
    {
        var alphaByte = (color >> 24) & 0xFF;
        var newAlpha = (uint)(alphaByte * _alpha);
        return (color & 0x00FFFFFF) | (newAlpha << 24);
    }
}
