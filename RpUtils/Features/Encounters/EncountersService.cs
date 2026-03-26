using RpUtils.Services;

namespace RpUtils.Features.Encounters;

public sealed class EncountersService
{
    private readonly HubConnectionService _hub;

    public EncountersService(HubConnectionService hub)
    {
        _hub = hub;

        _hub.OnConnected += connection =>
        {
            // Register SignalR event handlers here
        };
    }
}
