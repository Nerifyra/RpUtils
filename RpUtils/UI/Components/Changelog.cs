using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System.Collections.Generic;
using System.Numerics;

namespace RpUtils.UI.Components;

/// <summary>
/// The plugin's changelog. Each version is registered as a separate method
/// and added newest-first. Call <see cref="Draw"/> to render the version list.
/// </summary>
public sealed class Changelog
{
    public static readonly Changelog Instance = BuildChangelog();

    private static readonly Dictionary<ChangeEntryType, Vector4> EntryStyles = new()
    {
        [ChangeEntryType.Breaking] = Theme.RedColor,
        [ChangeEntryType.Feature] = Theme.PurpleColor,
        [ChangeEntryType.Fix] = Theme.WhiteColor,
    };

    private List<ChangeVersion> Versions { get; }

    internal Changelog(List<ChangeVersion> versions)
    {
        Versions = versions;
    }

    /// <summary>
    /// Renders all version sections as collapsible headers with colored entries.
    /// </summary>
    public void Draw()
    {
        for (var i = 0; i < Versions.Count; i++)
        {
            DrawVersion(Versions[i], isLatest: i == 0);
        }
    }

    // ── Version Definitions (newest first) ───────────────────────────────

    private static Changelog BuildChangelog()
    {
        var builder = new ChangelogBuilder();

        // Add new versions at the top
        AddVersion_0_7_1(builder);
        AddVersion_0_7_0(builder);
        AddVersion_0_5_1(builder);
        AddVersion_0_5_0(builder);

        return builder.Build();
    }

    private static void AddVersion_0_7_1(ChangelogBuilder builder)
    {
        builder.NextVersion("0.7.1")
            .Important("Added /roll and /rollcheck commands")
                .Detail("`/roll` allows more complex rolls with modifiers. e.g. `/roll 2d6+3` or `/roll 1d20-1`.")
                .Detail("`/roll` results return with a code that can be used to verify the result with `/rollcheck`, e.g. `/rollcheck 123456`.");
    }

    private static void AddVersion_0_7_0(ChangelogBuilder builder)
    {
        builder.NextVersion("0.7.0")
            .Important("Custom Waymarks added")
                .Detail("Custom waymarks are attached to a lobby.")
                .Detail("They can use an icon and label.")
                .Detail("Marker visibility for non DM lobby members can be toggled for hiding markers.")
            .Minor("Creating/joining a lobby now uses proper character homeworld")
            .Minor("Toolbar window visibility persisted.")
            .Minor("Sonar button now toggles /roleplaying when linked.")
            .Minor("Updated configuration for alerts.")
                .Detail("Alerts now include both a chat and audio component that can be configured independently.");
    }

    private static void AddVersion_0_5_1(ChangelogBuilder builder)
    {
        builder.NextVersion("0.5.1")
            .Important("Added option to link sonar location sharing to /roleplaying.")
                .Detail("Linking roleplay sonar location sharing can be toggled on and off in configuration menu.")
            .Minor("Fixes around multiple simulatenous rolls resulting in subsequent rolls not tracking.")
                .Detail("The roll collection is still a bit fragile while testing. Please report any similar issues!")
            .Minor("Requested initiative rolls can now be ended early.")
            .Minor("Fix for duplicated NPCs when editing encounters.")
            .Minor("Various visual fixes.");
    }

    private static void AddVersion_0_5_0(ChangelogBuilder builder)
    {
        builder
            .NextVersion("0.5.0")
                .Important("First pass at Lobbies")
                    .Detail("Lobbies are created and can be joined with a generated code.")
                    .Detail("Basic management features like kicking, renaming, and promoting members.")
                    .Detail("Display name is only for display in the plugin.")
                    .Detail("Character name should match in-game character to support features like automatically tracking rolls.")
                    .Detail("Non plugin users can be represented by adding a ghost player. Auto populated with current targets details.")
                    .Detail("Lobby members can be promoted to lobby moderators, allowing them to manage the lobby/encounters/rolls as well.")
                .Important("First pass at Encounters")
                    .Detail("Encounters represent combat instances. A single lobby can have multiple Encounters created by the owner/mods")
                    .Detail("Encounters can be modified to add or remove lobby members at any time.")
                    .Detail("NPCs can be added to Encounters.")
                    .Detail("Supports basic initiative and turn order. Adjusting the turn order value will reorder encounter members from highest to lowest.")
                .Important("First pass at Rolls feature")
                    .Detail("Lobby owner and moderators can initiate roll requests from Encounter members.")
                    .Detail("Roll requests can be edited to add or remove Encounter members.")
                    .Detail("Roll requests will listen to the initiators chat for rolls from the requested members to automatically populate results.")
                    .Detail("Requests for rolls from NPCs will use the requestors rolls to populate results, in order of initiative.")
                    .Detail("Roll requests can be set to populate initiative when configuring.")
                    .Detail("Roll requests can be set with a DC. Results will indicate pass or failure.")
                    .Detail("Chat alerts have been added for roll/initiative requested and results. Can be toggled on/off in configuration menu.")
                .Important("Added changelog")
                    .Detail("You're reading it! Wow! Now that we're getting to some more in depth features, figured it would be handy.")
                    .Detail("Can be toggled on/off to show on load for major updates.")
                .Important("Added buttons to show changelog and link to the support discord in the config menu.");
    }

    // ── Rendering ────────────────────────────────────────────────────────

    private static void DrawVersion(ChangeVersion version, bool isLatest)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, Theme.GoldColor);
        var flags = isLatest ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        var isOpen = ImGui.CollapsingHeader(version.Version, flags);
        ImGui.PopStyleColor();

        if (!isOpen) return;

        using (ImRaii.PushIndent())
        {
            foreach (var entry in version.Entries)
            {
                var color = EntryStyles.GetValueOrDefault(entry.Type, Theme.WhiteColor);

                ImGui.Bullet();
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TextWrapped($"{entry.Text}");
                ImGui.PopStyleColor();

                if (entry.Details.Count > 0)
                {
                    using (ImRaii.PushIndent())
                    {
                        foreach (var detail in entry.Details)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, Theme.WhiteColor);
                            ImGui.Bullet();
                            ImGui.SameLine();
                            ImGui.TextWrapped(detail);
                            ImGui.PopStyleColor();
                        }
                    }
                }
            }
        }

        ImGui.Spacing();
    }
}
