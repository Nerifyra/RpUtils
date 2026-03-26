using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Encounters.Models;

namespace RpUtils.Features.Encounters.UI;

internal class EncounterDetailTab
{
    public void Draw(string encounterId, EncounterState encounter)
    {
        using var tab = ImRaii.TabItem($"{encounter.Name}##{encounterId}");
        if (!tab.Success) return;

        ImGui.Text($"Round {encounter.RoundNumber}");
        foreach (var participant in encounter.Participants)
        {
            var prefix = participant.IsCurrent ? "> " : "  ";
            var initiative = participant.Initiative.HasValue ? $"[{participant.Initiative}] " : "";
            ImGui.Text($"{prefix}{initiative}{participant.DisplayName}");
        }
    }
}
