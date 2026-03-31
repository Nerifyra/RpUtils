using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;

namespace RpUtils.UI.Config;

internal static class LobbyConfigTab
{
    public static void Draw()
    {
        using var tab = ImRaii.TabItem("Lobbies");
        if (!tab.Success) return;

        var config = Plugin.Configuration;

        ImGui.Text("Chat Alerts");
        ImGui.Spacing();

        DrawToggle("Roll requested", config.RollRequestedChatAlert, v => config.RollRequestedChatAlert = v);
        DrawToggle("Roll results", config.RollResultsChatAlert, v => config.RollResultsChatAlert = v);
        DrawToggle("Initiative requested", config.InitiativeRequestedChatAlert, v => config.InitiativeRequestedChatAlert = v);
        DrawToggle("Initiative results", config.InitiativeResultsChatAlert, v => config.InitiativeResultsChatAlert = v);
    }

    private static void DrawToggle(string label, bool value, Action<bool> setter)
    {
        if (ImGui.Checkbox(label, ref value))
        {
            setter(value);
            Plugin.Configuration.Save();
        }
    }
}
