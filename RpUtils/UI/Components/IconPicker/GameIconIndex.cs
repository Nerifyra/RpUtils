using Dalamud.Interface.Textures;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.UI.Components.IconPicker;

// Unsure if this is the approach we want in the long run, but works okay enough for now.
public static class GameIconIndex
{
    private const int MaxIconId = 250_000;

    private static readonly Task<List<uint>> _buildTask = Task.Run(Build);

    public static bool IsReady => _buildTask.IsCompletedSuccessfully;

    public static IReadOnlyList<uint> Icons =>
        IsReady ? _buildTask.Result : Array.Empty<uint>();

    public static int? FindNearest(uint targetIconId)
    {
        if (!IsReady) return null;
        var list = _buildTask.Result;
        if (list.Count == 0) return null;

        int lo = 0, hi = list.Count - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            if (list[mid] == targetIconId) return mid;
            if (list[mid] < targetIconId) lo = mid + 1;
            else hi = mid - 1;
        }
        return Math.Min(lo, list.Count - 1);
    }

    private static List<uint> Build()
    {
        var ids = new List<uint>(capacity: 30_000);
        for (var id = 0u; id < MaxIconId; id++)
        {
            if (Plugin.TextureProvider.TryGetIconPath(new GameIconLookup(id), out _))
                ids.Add(id);
        }
        Plugin.Log.Debug($"GameIconIndex: indexed {ids.Count} icons.");
        return ids;
    }
}
