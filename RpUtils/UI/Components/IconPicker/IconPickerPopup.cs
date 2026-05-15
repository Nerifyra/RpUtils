using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;

namespace RpUtils.UI.Components.IconPicker;

public sealed class IconPickerPopup
{
    private const float PopupMinWidth = 540f;
    private const float DialogButtonWidth = 80f;

    private readonly string _popupId;
    private readonly IconPickerComponent _picker = new();

    private Action<uint>? _onConfirm;
    private bool _openPopup;

    public IconPickerPopup()
    {
        _popupId = $"Pick icon##IconPickerPopup_{GetHashCode()}";
    }

    public void Open(uint currentIconId, Action<uint> onConfirm)
    {
        _picker.SelectedIconId = currentIconId;
        _onConfirm = onConfirm;
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
        if (!ImGui.BeginPopupModal(_popupId, ref open, ImGuiWindowFlags.AlwaysAutoResize))
            return;

        if (!open) _onConfirm = null;

        _picker.Draw();

        ImGui.Spacing();
        if (ImGui.Button("OK", new Vector2(DialogButtonWidth, 0))) Confirm();
        ImGui.SameLine();
        if (ImGui.Button("Cancel", new Vector2(DialogButtonWidth, 0))) Dismiss();

        ImGui.EndPopup();
    }

    private void Confirm()
    {
        _onConfirm?.Invoke(_picker.SelectedIconId);
        Dismiss();
    }

    private void Dismiss()
    {
        _onConfirm = null;
        ImGui.CloseCurrentPopup();
    }
}
