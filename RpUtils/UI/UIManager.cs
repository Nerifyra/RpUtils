using Dalamud.Interface.Windowing;
using RpUtils.Features.Lobbies.UI;
using RpUtils.Features.Sonar.UI;
using RpUtils.UI.Windows;
using System;

namespace RpUtils.UI;

public sealed class UIManager : IDisposable
{
    private readonly WindowSystem _windowSystem = new("RpUtils");
    public Fonts Fonts { get; }

    private readonly ConfigWindow _configWindow;
    private readonly ToolbarWindow _toolbarWindow;
    private readonly LobbiesWindow _lobbiesWindow;
    private readonly ShareLocationWindow _shareLocationWindow;
    private readonly FindRoleplayWindow _findRoleplayWindow;
    private readonly ChangelogWindow _changelogWindow;

    public UIManager()
    {
        // Fonts
        Fonts = new Fonts();
        Fonts.Initialize();

        // Windows
        _configWindow = new ConfigWindow();
        _lobbiesWindow = new LobbiesWindow();
        _shareLocationWindow = new ShareLocationWindow();
        _findRoleplayWindow = new FindRoleplayWindow();
        _changelogWindow = new ChangelogWindow();
        _toolbarWindow = new ToolbarWindow(
            () => _shareLocationWindow.Toggle(),
            () => _findRoleplayWindow.Toggle(),
            () => _lobbiesWindow.Toggle(),
            () => _configWindow.Toggle()
        );

        _windowSystem.AddWindow(_configWindow);
        _windowSystem.AddWindow(_lobbiesWindow);
        _windowSystem.AddWindow(_shareLocationWindow);
        _windowSystem.AddWindow(_findRoleplayWindow);
        _windowSystem.AddWindow(_toolbarWindow);
        _windowSystem.AddWindow(_changelogWindow);

        // Auto-open changelog on major/minor version change (if enabled)
        var currentRelease = PluginConstants.GetReleaseVersion(PluginConstants.PluginVersion);
        if (Plugin.Configuration.ShowChangelogOnUpdate
            && Plugin.Configuration.LastSeenChangelogVersion != currentRelease)
            _changelogWindow.IsOpen = true;
    }

    public void Draw() => _windowSystem.Draw();
    public void ToggleConfigWindow() => _configWindow.Toggle();
    public void ToggleToolbarWindow() => _toolbarWindow.Toggle();
    public void ToggleChangelogWindow() => _changelogWindow.Toggle();


    public void Dispose()
    {
        _windowSystem.RemoveAllWindows();
        Fonts.Dispose();
    }
}
