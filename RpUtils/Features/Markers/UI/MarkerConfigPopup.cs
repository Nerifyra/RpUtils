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
    private readonly IconPickerPopup _iconPickerPopup = new();

    private Marker? _target;
    private uint _iconId;
    private string _label = string.Empty;
    private bool _isVisible;
    private float _size;
    private bool _openPopup;

    public MarkerConfigPopup()
    {
        _popupId = $"Configure marker##MarkerConfigPopup_{GetHashCode()}";
    }

    public void Open(Marker marker)
    {
        _target = marker;
        _iconId = marker.IconId;
        _label = marker.Label;
        _isVisible = marker.IsVisible;
        _size = marker.Size;
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

        ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##label", "Label", ref _label, 64);

        ImGui.Spacing();
        ImGui.SetNextItemWidth(-1);
        ImGui.SliderFloat("##size", ref _size, 0.5f, 5f, "Size %.2fx");
    }

    private void Confirm()
    {
        if (_target is not null)
        {
            _target.IconId = _iconId;
            _target.Label = _label;
            _target.IsVisible = _isVisible;
            _target.Size = _size;
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
