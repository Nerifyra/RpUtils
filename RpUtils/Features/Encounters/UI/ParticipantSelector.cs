using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using RpUtils.Features.Lobbies.Models;
using System.Collections.Generic;
using System.Linq;

namespace RpUtils.Features.Encounters.UI;

internal class ParticipantSelector
{
    private readonly string _id;
    private readonly HashSet<string> _selectedPlayerIds = [];

    public IReadOnlySet<string> SelectedPlayerIds => _selectedPlayerIds;

    public ParticipantSelector(string id)
    {
        _id = id;
    }

    public void SelectAll(IEnumerable<string> playerIds)
    {
        _selectedPlayerIds.Clear();
        _selectedPlayerIds.UnionWith(playerIds);
    }

    public void Clear()
    {
        _selectedPlayerIds.Clear();
    }

    public void Draw(IReadOnlyList<LobbyMember> members, float height = 0)
    {
        var size = new System.Numerics.Vector2(0, height > 0 ? height : members.Count * ImGui.GetFrameHeightWithSpacing());
        using var child = ImRaii.Child($"ParticipantSelector##{_id}", size, true);
        if (!child.Success) return;

        foreach (var member in members)
        {
            var isSelected = _selectedPlayerIds.Contains(member.PlayerId);
            if (ImGui.Checkbox($"{member.DisplayName}##{member.PlayerId}", ref isSelected))
            {
                if (isSelected)
                    _selectedPlayerIds.Add(member.PlayerId);
                else
                    _selectedPlayerIds.Remove(member.PlayerId);
            }
        }
    }
}
