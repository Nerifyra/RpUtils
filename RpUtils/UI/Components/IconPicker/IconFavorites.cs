using System.Collections.Generic;

namespace RpUtils.UI.Components.IconPicker;

public static class IconFavorites
{
    private static List<uint> Store => Plugin.Configuration.FavoriteIcons;

    public static IReadOnlyList<uint> All => Store;
    public static int Count => Store.Count;

    public static bool IsFavorite(uint iconId) => Store.Contains(iconId);

    public static void Toggle(uint iconId)
    {
        if (!Store.Remove(iconId))
            Store.Add(iconId);
        Plugin.Configuration.Save();
    }
}
