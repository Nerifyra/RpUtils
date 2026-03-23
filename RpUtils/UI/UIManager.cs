using Dalamud.Interface.Windowing;
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
    private readonly LobbyWindow _lobbyWindow;
    private readonly ShareLocationWindow _shareLocationWindow;
    private readonly FindRoleplayWindow _findRoleplayWindow;

    public UIManager()
    {
        // Fonts
        Fonts = new Fonts();
        Fonts.Initialize();

        // Windows
        _configWindow = new ConfigWindow();
        _lobbyWindow = new LobbyWindow();
        _shareLocationWindow = new ShareLocationWindow();
        _findRoleplayWindow = new FindRoleplayWindow();
        _toolbarWindow = new ToolbarWindow(
            () => _shareLocationWindow.Toggle(),
            () => _findRoleplayWindow.Toggle(),
            () => _lobbyWindow.Toggle(),
            () => _configWindow.Toggle()
        );

        _windowSystem.AddWindow(_configWindow);
        _windowSystem.AddWindow(_lobbyWindow);
        _windowSystem.AddWindow(_shareLocationWindow);
        _windowSystem.AddWindow(_findRoleplayWindow);
        _windowSystem.AddWindow(_toolbarWindow);
    }

    public void Draw() => _windowSystem.Draw();
    public void ToggleConfigWindow() => _configWindow.Toggle();
    public void ToggleToolbarWindow() => _toolbarWindow.Toggle();

    public void Dispose()
    {
        _windowSystem.RemoveAllWindows();
        Fonts.Dispose();
    }
}
