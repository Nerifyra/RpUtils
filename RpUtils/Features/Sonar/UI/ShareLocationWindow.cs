using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Features.Sonar.Models;
using RpUtils.UI;
using System.Threading.Tasks;

namespace RpUtils.Features.Sonar.UI;

internal class ShareLocationWindow : Window
{
    public ShareLocationWindow() : base("Share Roleplay Location")
    {
        Flags = Theme.CompactWindowFlags;
        IsOpen = false;
    }

    private void DrawActivitySelection()
    {
        var sonar = Plugin.Sonar;
        var selected = SonarActivity.DisplayName(sonar.CurrentActivity);

        using var combo = ImRaii.Combo("##RoleplayActivity", selected);
        if (!combo) return;

        foreach (var activity in SonarActivity.All)
        {
            var isSelected = activity == sonar.CurrentActivity;
            if (ImGui.Selectable(SonarActivity.DisplayName(activity), isSelected))
            {
                sonar.SetActivity(activity);
            }
            if (isSelected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }
    }

    public override void Draw()
    {
        var sonar = Plugin.Sonar;
        var isSharing = sonar.IsSharingLocation;
        using var disabled = ImRaii.Disabled(!Plugin.ConnectionStatus.IsConnected);
        if (ImGui.Checkbox("Share Roleplay Location", ref isSharing))
        {
            Task.Run(async () =>
            {
                if (isSharing)
                    await sonar.StartSharing();
                else
                    await sonar.StopSharing();
            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Enabling will anonymously share your current location, indicating you are roleplaying and open to walkups.");
        }

        DrawActivitySelection();
    }
}
