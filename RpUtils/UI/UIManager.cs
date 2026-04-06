using Dalamud.Interface.Windowing;
using RpUtils.Features.Lobbies.UI;
using RpUtils.Features.Sonar.UI;
using RpUtils.UI.Windows;
using System;
using System.Collections.Generic;

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

    private readonly Dictionary<string, LobbyDetailWindow> _lobbyDetailWindows = [];

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

        // Subscribe to lobby lifecycle events
        Plugin.Lobbies.OnLobbyEntered += OpenLobbyDetail;
        Plugin.Lobbies.OnLobbyRemoved += CloseLobbyDetail;
    }

    public void Draw() => _windowSystem.Draw();
    public void ToggleConfigWindow() => _configWindow.Toggle();
    public void ToggleToolbarWindow() => _toolbarWindow.Toggle();
    public void ToggleChangelogWindow() => _changelogWindow.Toggle();

    public void OpenLobbyDetail(string lobbyId)
    {
        if (_lobbyDetailWindows.TryGetValue(lobbyId, out var existing))
        {
            existing.IsOpen = true;
            return;
        }

        var window = new LobbyDetailWindow(lobbyId);
        _lobbyDetailWindows[lobbyId] = window;
        _windowSystem.AddWindow(window);
    }

    public void CloseLobbyDetail(string lobbyId)
    {
        if (!_lobbyDetailWindows.Remove(lobbyId, out var window)) return;
        window.IsOpen = false;
        _windowSystem.RemoveWindow(window);
    }

    public void Dispose()
    {
        Plugin.Lobbies.OnLobbyEntered -= OpenLobbyDetail;
        Plugin.Lobbies.OnLobbyRemoved -= CloseLobbyDetail;
        _windowSystem.RemoveAllWindows();
        Fonts.Dispose();
    }
}
