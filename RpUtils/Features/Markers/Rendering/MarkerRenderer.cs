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

    // Label background pill — solid contrast-colored rect behind the text.
    private const float LabelBgPaddingX = 4f;
    private const float LabelBgPaddingY = 2f;
    private const float LabelBgRounding = 3f;

    // Hidden-indicator glyph stays white; the per-marker color only affects dot, ring, and label.
    private const uint FillColor = 0xFFFFFFFF;

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

        var color = NormalizeColor(marker.Color);
        var borderColor = ComputeBorderColor(color);

        DrawDot(marker.WorldPos, color, borderColor);
        DrawRing(marker.WorldPos, waveCenterAngle, RingRadius * marker.Size, color, borderColor);
        DrawOverlayStack(marker.WorldPos, marker.IconId, marker.Label, color, borderColor, drawHiddenIndicator: !marker.IsVisible);
    }

    private void DrawReticle(Marker placingMarker, float waveCenterAngle)
    {
        _alpha = 1f;

        if (!Plugin.GameGui.ScreenToWorld(ImGui.GetMousePos(), out var worldPos)) return;

        var color = NormalizeColor(placingMarker.Color);
        var borderColor = ComputeBorderColor(color);

        DrawDot(worldPos, color, borderColor);
        DrawRing(worldPos, waveCenterAngle, RingRadius * placingMarker.Size, color, borderColor);
        DrawOverlayStack(worldPos, placingMarker.IconId, placingMarker.Label, color, borderColor, drawHiddenIndicator: false);
    }

    private void DrawOverlayStack(Vector3 foot, uint iconId, string label, uint color, uint borderColor, bool drawHiddenIndicator)
    {
        var iconAnchor = foot + new Vector3(0, IconHeight, 0);
        if (!Plugin.GameGui.WorldToScreen(iconAnchor, out var iconScreen)) return;

        var iconSize = Plugin.Configuration.MarkerIconSize;
        DrawIcon(iconScreen, iconId, iconSize);

        // Stack cursor moves upward as each element draws above the previous.
        var topY = iconScreen.Y - iconSize / 2f;
        DrawLabel(iconScreen.X, ref topY, label, color, borderColor);
        if (drawHiddenIndicator) DrawHiddenIndicator(iconScreen.X, ref topY);
    }

    private void DrawDot(Vector3 pos, uint color, uint borderColor)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddCircleFilled(pos, DotBorderRadius, Tint(borderColor));
        drawList.AddCircleFilled(pos, DotRadius, Tint(color));
    }

    private void DrawRing(Vector3 origin, float waveCenterAngle, float radius, uint color, uint borderColor)
    {
        DrawWaveArc(origin, waveCenterAngle, radius, color, borderColor);
        DrawWaveArc(origin, waveCenterAngle + MathF.PI, radius, color, borderColor);
    }

    private void DrawWaveArc(Vector3 origin, float centerAngle, float radius, uint color, uint borderColor)
    {
        var drawList = PctService.GetDrawList();
        drawList.AddArc(origin, radius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(borderColor), RingSegments, WaveArcBorderThickness);
        drawList.AddArc(origin, radius,
            centerAngle - WaveArcHalfWidthRad,
            centerAngle + WaveArcHalfWidthRad,
            Tint(color), RingSegments, WaveArcThickness);
    }

    private void DrawIcon(Vector2 screenCenter, uint iconId, float size)
    {
        IconDisplay.DrawOn(ImGui.GetBackgroundDrawList(), iconId, screenCenter, new Vector2(size, size), Tint(IconTint));
    }

    private void DrawLabel(float xCenter, ref float topY, string label, uint color, uint borderColor)
    {
        if (string.IsNullOrEmpty(label)) return;

        var scale = Plugin.Configuration.MarkerLabelScale;
        var font = ImGui.GetFont();
        var fontSize = font.FontSize * scale;
        var textSize = ImGui.CalcTextSize(label) * scale;

        var pos = new Vector2(xCenter - textSize.X / 2f, topY - StackPaddingPx - textSize.Y);
        var drawList = ImGui.GetBackgroundDrawList();

        var padding = new Vector2(LabelBgPaddingX, LabelBgPaddingY);
        drawList.AddRectFilled(pos - padding, pos + textSize + padding, Tint(borderColor), LabelBgRounding);

        drawList.AddText(font, fontSize, pos, Tint(color), label);
        topY = pos.Y - padding.Y;
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

    /// <summary>Force the alpha byte to opaque so a misconfigured / unmigrated marker can't render invisible.</summary>
    private static uint NormalizeColor(uint color) => (color & 0x00FFFFFFu) | 0xFF000000u;

    // Softened contrast pair — feels less harsh against colored fills than pure black/white.
    private const uint BorderDark = 0xFF2A2A2Au;  // charcoal
    private const uint BorderLight = 0xFFE6E6E6u; // off-white

    /// <summary>Pick charcoal or off-white as the contrast border based on the fill color's perceived brightness (WCAG luminance).</summary>
    private static uint ComputeBorderColor(uint fillColor)
    {
        var r = (fillColor & 0xFF) / 255f;
        var g = ((fillColor >> 8) & 0xFF) / 255f;
        var b = ((fillColor >> 16) & 0xFF) / 255f;
        var luminance = 0.299f * r + 0.587f * g + 0.114f * b;
        return luminance > 0.5f ? BorderDark : BorderLight;
    }
}
