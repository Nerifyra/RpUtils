using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace RpUtils.Features.Encounters.UI;

internal class EncountersTab
{
    private readonly string _lobbyId;

    public EncountersTab(string lobbyId)
    {
        _lobbyId = lobbyId;
    }

    public void Draw()
    {
        using var tab = ImRaii.TabItem($"Encounters##{_lobbyId}");
        if (!tab.Success) return;

        ImGui.Text("Coming soon...");
    }
}
