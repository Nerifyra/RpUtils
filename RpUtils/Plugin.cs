using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RpUtils.Features.Sonar;
using RpUtils.Services;
using RpUtils.UI;
using System.Threading.Tasks;

namespace RpUtils;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;

    internal static Configuration Configuration { get; private set; } = null!;
    internal static IConnectionStatus ConnectionStatus { get; private set; } = null!;
    internal static ISonarController Sonar { get; private set; } = null!;
    internal static UIManager UI { get; private set; } = null!;

    private const string CommandName = "/rputils";

    private readonly HubConnectionService _hub;
    private readonly SonarService _sonarService;
    private readonly SonarController _sonarController;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Services
        _hub = new HubConnectionService();
        _sonarService = new SonarService(_hub);
        _sonarController = new SonarController(_sonarService);

        ConnectionStatus = _hub;
        Sonar = _sonarController;

        // UI
        UI = new UIManager();

        // Commands
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the display of the Rp Utils toolbar."
        });

        PluginInterface.UiBuilder.Draw += UI.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += UI.ToggleConfigWindow;
        PluginInterface.UiBuilder.OpenMainUi += UI.ToggleToolbarWindow;

        Task.Run(async () => await _hub.ConnectAsync());
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= UI.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= UI.ToggleConfigWindow;
        PluginInterface.UiBuilder.OpenMainUi -= UI.ToggleToolbarWindow;

        CommandManager.RemoveHandler(CommandName);

        UI.Dispose();
        _sonarController.Dispose();
        _hub.DisposeAsync().AsTask().Wait();
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug($"OnCommand {command}: {args}");
        UI.ToggleToolbarWindow();
    }
}