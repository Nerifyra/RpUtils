using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Linq;

namespace RpUtils.UI.Config;

internal static class LobbyConfigTab
{
    // FFXIV's built-in chat sounds, matching the in-game <se.N> tokens.
    private const uint MinSoundId = 1;
    private const uint MaxSoundId = 16;
    private static readonly string[] SoundNames =
        Enumerable.Range((int)MinSoundId, (int)(MaxSoundId - MinSoundId + 1))
            .Select(i => $"Sound {i}")
            .ToArray();

    public static void Draw()
    {
        using var tab = ImRaii.TabItem("Lobbies");
        if (!tab.Success) return;

        var config = Plugin.Configuration;

        ImGui.Text("Alerts");
        ImGui.Spacing();
        DrawAlertsTable(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Markers");
        ImGui.Spacing();

        var iconSize = config.MarkerIconSize;
        if (ImGui.SliderFloat("Icon size", ref iconSize, 12f, 96f, "%.0f px"))
            config.MarkerIconSize = iconSize;
        if (ImGui.IsItemDeactivatedAfterEdit())
            Plugin.Configuration.Save();

        var labelScale = config.MarkerLabelScale;
        if (ImGui.SliderFloat("Label scale", ref labelScale, 0.5f, 3f, "%.2fx"))
            config.MarkerLabelScale = labelScale;
        if (ImGui.IsItemDeactivatedAfterEdit())
            Plugin.Configuration.Save();
    }

    private static void DrawToggle(string label, bool value, Action<bool> setter)
    {
        if (ImGui.Checkbox(label, ref value))
        {
            setter(value);
            Plugin.Configuration.Save();
        }
    }

    private static void DrawAlertsTable(Configuration config)
    {
        var flags = ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH;
        using var table = ImRaii.Table("RollAlerts", 4, flags);
        if (!table.Success) return;

        ImGui.TableSetupColumn("Alert", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Chat", ImGuiTableColumnFlags.WidthFixed, 30);
        ImGui.TableSetupColumn("Sound", ImGuiTableColumnFlags.WidthFixed, 40);
        ImGui.TableSetupColumn("Sound effect", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableHeadersRow();

        DrawAlertRow("Roll requested",
            config.RollRequestedChatAlert, v => config.RollRequestedChatAlert = v,
            config.RollRequestedSoundAlert, v => config.RollRequestedSoundAlert = v,
            config.RollRequestedSoundId, v => config.RollRequestedSoundId = v);

        DrawAlertRow("Roll results",
            config.RollResultsChatAlert, v => config.RollResultsChatAlert = v,
            config.RollResultsSoundAlert, v => config.RollResultsSoundAlert = v,
            config.RollResultsSoundId, v => config.RollResultsSoundId = v);
    }

    private static void DrawAlertRow(
        string label,
        bool chatEnabled, Action<bool> setChatEnabled,
        bool soundEnabled, Action<bool> setSoundEnabled,
        uint soundId, Action<uint> setSoundId)
    {
        using var rowId = ImRaii.PushId(label);
        ImGui.TableNextRow();

        // Alert name
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);

        // Chat toggle
        ImGui.TableNextColumn();
        DrawToggle("##chat", chatEnabled, setChatEnabled);

        // Sound toggle
        ImGui.TableNextColumn();
        DrawToggle("##sound", soundEnabled, setSoundEnabled);

        // Sound effect dropdown + preview
        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!soundEnabled))
        {
            var idx = (int)(soundId - MinSoundId);
            if (idx < 0 || idx >= SoundNames.Length) idx = 0;

            ImGui.SetNextItemWidth(-(ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X));
            if (ImGui.Combo("##sound_id", ref idx, SoundNames, SoundNames.Length))
            {
                setSoundId((uint)idx + MinSoundId);
                Plugin.Configuration.Save();
            }
        }

        ImGui.SameLine();
        if (ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            UIGlobals.PlayChatSoundEffect(soundId);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Preview");
    }
}
