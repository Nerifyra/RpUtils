using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using System;

namespace RpUtils.UI;

public sealed class Fonts : IDisposable
{
    public IFontHandle Header { get; private set; } = null!;
    public IFontHandle Small { get; private set; } = null!;

    public void Initialize()
    {
        Header = Plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 18f));
        Small = Plugin.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(new GameFontStyle(GameFontFamily.Axis, 10f));
    }

    public void Dispose()
    {
        Header?.Dispose();
        Small?.Dispose();
    }
}
