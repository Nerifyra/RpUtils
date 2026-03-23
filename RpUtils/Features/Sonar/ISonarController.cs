using RpUtils.Features.Sonar.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Features.Sonar;

public interface ISonarController
{
    bool IsSharingLocation { get; }
    string CurrentActivity { get; }

    Task ToggleSharing();
    Task StartSharing();
    Task StopSharing();
    Task SetActivity(string activity);

    IReadOnlyList<WorldMapGroup> GroupedCounts { get; }
    int WatchingCount { get; }
    bool IsFetchingCounts { get; }
    Task RefreshWorldMapCounts();
}