using System;

namespace RpUtils.Features.Encounters;

public interface IEncountersController
{
    event Action? OnStateChanged;
}
