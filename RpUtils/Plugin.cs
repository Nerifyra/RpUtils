using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Pictomancy;
using RpUtils.Features.Encounters;
using RpUtils.Features.Lobbies;
using RpUtils.Features.Markers;
using RpUtils.Features.Rolls;
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
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    internal static Configuration Configuration { get; private set; } = null!;
    internal static IConnectionStatus ConnectionStatus { get; private set; } = null!;
    internal static ISonarController Sonar { get; private set; } = null!;
    internal static ILobbiesController Lobbies { get; private set; } = null!;
    internal static IEncountersController Encounters { get; private set; } = null!;
    internal static IRollsController Rolls { get; private set; } = null!;
    internal static IMarkersController Markers {  get; private set; } = null!;
    internal static UIManager UI { get; private set; } = null!;

    private const string CommandName = "/rputils";
    private const string RollCommandName = "/roll";
    private const string RollCheckCommandName = "/rollcheck";

    private readonly PctContext _pictomancy;
    private readonly HubConnectionService _hub;
    private readonly SonarService _sonarService;
    private readonly SonarController _sonarController;
    private readonly LobbiesService _lobbiesService;
    private readonly LobbiesController _lobbiesController;
    private readonly EncountersService _encountersService;
    private readonly EncountersController _encountersController;
    private readonly RollsService _rollsService;
    private readonly RollsController _rollsController;
    private readonly MarkersService _markersService;
    private readonly MarkersController _markersController;
    private readonly ChatRollListener _chatRollListener;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // Services
        _hub = new HubConnectionService();
        ConnectionStatus = _hub;

        _pictomancy = PctService.Initialize(PluginInterface);

        _sonarService = new SonarService(_hub);
        _sonarController = new SonarController(_sonarService);
        Sonar = _sonarController;

        _lobbiesService = new LobbiesService(_hub);
        _lobbiesController = new LobbiesController(_lobbiesService);
        Lobbies = _lobbiesController;

        _encountersService = new EncountersService(_hub);
        _encountersController = new EncountersController(_encountersService);
        Encounters = _encountersController;

        _rollsService = new RollsService(_hub);
        _rollsController = new RollsController(_rollsService);
        Rolls = _rollsController;

        _markersService = new MarkersService(_hub);
        _markersController = new MarkersController(_markersService);
        Markers = _markersController;

        _chatRollListener = new ChatRollListener();

        // UI
        UI = new UIManager();

        // Commands
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the display of the Rp Utils toolbar."
        });

        CommandManager.AddHandler(RollCommandName, new CommandInfo(OnRollCommand)
        {
            HelpMessage = "Roll dice, e.g. /roll 3d8+5"
        });
        CommandManager.AddHandler(RollCheckCommandName, new CommandInfo(OnRollCheckCommand)
        {
            HelpMessage = "Verify a roll is genuine by id, e.g. /rollcheck K7M2QF"
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
        CommandManager.RemoveHandler(RollCommandName);
        CommandManager.RemoveHandler(RollCheckCommandName);

        UI.Dispose();
        _chatRollListener.Dispose();
        _rollsController.Dispose();
        _markersController.Dispose();
        _encountersController.Dispose();
        _lobbiesController.Dispose();
        _sonarController.Dispose();
        _hub.DisposeAsync().AsTask().Wait();
        _pictomancy.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug($"OnCommand {command}: {args}");
        UI.ToggleToolbarWindow();
    }

    private void OnRollCommand(string command, string args)
    {
        Log.Debug($"OnRollCommand {command}: {args}");
        Rolls.GenerateRoll(args);
    }

    private void OnRollCheckCommand(string command, string args)
    {
        Log.Debug($"OnRollCheckCommand {command}: {args}");
        Rolls.RollCheck(args);
    }
}