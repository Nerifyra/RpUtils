using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Features.Sonar.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RpUtils.Features.Sonar.UI;

internal class FindRoleplayWindow : Window
{
    private const ImGuiTableFlags TreeTableFlags =
        ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH |
        ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBody |
        ImGuiTableFlags.RowBg;

    private readonly Stopwatch _refreshTimer = new();
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(15);

    public FindRoleplayWindow() : base("Currently Roleplaying...")
    {
        IsOpen = false;
    }

    public override void OnOpen()
    {
        Task.Run(async () => await Plugin.Sonar.RefreshWorldMapCounts());
        _refreshTimer.Restart();
    }

    public override void OnClose()
    {
        _refreshTimer.Stop();
    }

    public override void Draw()
    {
        var sonar = Plugin.Sonar;

        if (_refreshTimer.Elapsed >= _refreshInterval)
        {
            _refreshTimer.Restart();
            Task.Run(async () => await sonar.RefreshWorldMapCounts());
        }

        ImGui.Separator();

        if (sonar.IsFetchingCounts && sonar.GroupedCounts.Count == 0)
        {
            ImGui.Text("Loading...");
            return;
        }

        if (sonar.GroupedCounts.Count == 0)
        {
            ImGui.Text("No active roleplay.");
            return;
        }

        using var table = ImRaii.Table("Find Roleplay", 2, TreeTableFlags);
        if (!table) return;

        ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableHeadersRow();

        foreach (var world in sonar.GroupedCounts)
        {
            DrawWorldNode(world);
        }
    }

    private void DrawWorldNode(WorldMapGroup world)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        using var worldNode = ImRaii.TreeNode(world.WorldName, ImGuiTreeNodeFlags.SpanFullWidth);
        ImGui.TableNextColumn();
        ImGui.Text(world.TotalCount.ToString());

        if (!worldNode) return;

        foreach (var map in world.Maps)
        {
            DrawMapNode(map);
        }
    }

    private void DrawMapNode(MapActivityGroup map)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        using var mapNode = ImRaii.TreeNode(map.MapName, ImGuiTreeNodeFlags.SpanFullWidth);
        ImGui.TableNextColumn();
        ImGui.Text(map.TotalCount.ToString());

        if (!mapNode) return;

        foreach (var activity in map.Activities)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TreeNodeEx(SonarActivity.DisplayName(activity.Activity),
                ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
            ImGui.TableNextColumn();
            ImGui.Text(activity.Count.ToString());
        }
    }
}
