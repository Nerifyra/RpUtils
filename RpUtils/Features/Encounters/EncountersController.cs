using System;

namespace RpUtils.Features.Encounters;

public sealed class EncountersController : IEncountersController, IDisposable
{
    private readonly EncountersService _service;

    public event Action? OnStateChanged;

    public EncountersController(EncountersService service)
    {
        _service = service;
    }

    public void Dispose()
    {
    }
}
