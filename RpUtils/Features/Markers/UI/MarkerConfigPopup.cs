using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Markers.Models;
using RpUtils.UI.Components;
using RpUtils.UI.Components.IconPicker;
using System.Numerics;

namespace RpUtils.Features.Markers.UI;

internal sealed class MarkerConfigPopup
{
    private const float PopupMinWidth = 320f;
    private const float DialogButtonWidth = 80f;
    private static readonly Vector2 ButtonSize = new(32, 32);

    private readonly string _popupId;
    private readonly string _colorPopupId;
    private readonly IconPickerPopup _iconPickerPopup = new();

    private Marker? _target;
    private uint _iconId;
    private string _label = string.Empty;
    private bool _isVisible;
    private float _size;
    private uint _color;
    private bool _openPopup;

    public MarkerConfigPopup()
    {
        _popupId = $"Configure marker##MarkerConfigPopup_{GetHashCode()}";
        _colorPopupId = $"##colorPopup_{GetHashCode()}";
    }

    public void Open(Marker marker)
    {
        _target = marker;
        _iconId = marker.IconId;
        _label = marker.Label;
        _isVisible = marker.IsVisible;
        _size = marker.Size;
        _color = marker.Color;
        _openPopup = true;
    }

    public void Draw()
    {
        if (_openPopup)
        {
            ImGui.OpenPopup(_popupId);
            _openPopup = false;
        }

        ImGui.SetNextWindowSizeConstraints(new Vector2(PopupMinWidth, 0), new Vector2(float.MaxValue, float.MaxValue));
        var open = true;
        if (ImGui.BeginPopupModal(_popupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (!open) _target = null;

            DrawFields();

            ImGui.Spacing();
            if (ImGui.Button("OK", new Vector2(DialogButtonWidth, 0))) Confirm();
            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(DialogButtonWidth, 0))) Dismiss();

            // Draw the nested icon picker INSIDE this modal's scope. If we drew it after
            // EndPopup, its OpenPopup call would fire at the root popup stack and ImGui
            // would close us out instead of stacking the picker on top.
            _iconPickerPopup.Draw();

            ImGui.EndPopup();
        }
    }

    private void DrawFields()
    {
        if (IconDisplay.DrawButton(_iconId, ButtonSize))
            _iconPickerPopup.Open(_iconId, id => _iconId = id);

        ImGui.SameLine();
        var visIcon = _isVisible ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            // Scale the glyph to fit the larger button — otherwise it renders at the font's native
            // size and looks tiny inside a 32x32 frame. The 0.7 factor leaves a bit of padding.
            ImGui.SetWindowFontScale(ButtonSize.Y / ImGui.GetFontSize() * 0.7f);
            if (ImGui.Button(visIcon.ToIconString(), ButtonSize))
                _isVisible = !_isVisible;
            ImGui.SetWindowFontScale(1f);
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(_isVisible ? "Hide marker" : "Show marker");

        ImGui.SameLine();
        DrawColorButton();

        ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##label", "Label", ref _label, 64);

        ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##size", ref _size, 0.5f, 5f, "Size %.2fx");
    }

    private void DrawColorButton()
    {
        var rgb = ColorToVector3(_color);
        if (ImGui.ColorButton("##color", new Vector4(rgb, 1f), ImGuiColorEditFlags.NoTooltip, ButtonSize))
            ImGui.OpenPopup(_colorPopupId);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Color");

        if (ImGui.BeginPopup(_colorPopupId))
        {
            if (ImGui.ColorPicker3("##colorPicker", ref rgb, ImGuiColorEditFlags.NoLabel | ImGuiColorEditFlags.NoSidePreview))
                _color = Vector3ToColor(rgb);
            ImGui.EndPopup();
        }
    }

    private static Vector3 ColorToVector3(uint color)
    {
        var r = (color & 0xFF) / 255f;
        var g = ((color >> 8) & 0xFF) / 255f;
        var b = ((color >> 16) & 0xFF) / 255f;
        return new Vector3(r, g, b);
    }

    private static uint Vector3ToColor(Vector3 v)
    {
        var r = (uint)(System.Math.Clamp(v.X, 0f, 1f) * 255f);
        var g = (uint)(System.Math.Clamp(v.Y, 0f, 1f) * 255f);
        var b = (uint)(System.Math.Clamp(v.Z, 0f, 1f) * 255f);
        return 0xFF000000u | (b << 16) | (g << 8) | r;
    }

    private void Confirm()
    {
        if (_target is not null)
        {
            _target.IconId = _iconId;
            _target.Label = _label;
            _target.IsVisible = _isVisible;
            _target.Size = _size;
            _target.Color = _color;
            _ = Plugin.Markers.UpdateMarker(_target);
        }
        Dismiss();
    }

    private void Dismiss()
    {
        _target = null;
        ImGui.CloseCurrentPopup();
    }
}
