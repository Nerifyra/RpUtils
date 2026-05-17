using Dalamud.Configuration;
using System;

namespace RpUtils;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool EnableRpUtils { get; set; } = true;
    public bool ShowToolbar { get; set; } = true;
    public bool LinkSonarSharingToRoleplayingStatus { get; set; } = true;

    // ── Roll Alerts ───────────────────────────────────────────────────
    public bool RollRequestedChatAlert { get; set; } = true;
    public bool RollRequestedSoundAlert { get; set; } = false;
    public uint RollRequestedSoundId { get; set; } = 1;

    public bool RollResultsChatAlert { get; set; } = true;
    public bool RollResultsSoundAlert { get; set; } = false;
    public uint RollResultsSoundId { get; set; } = 1;

    // ── Markers ───────────────────────────────────────────────────────
    public float MarkerIconSize { get; set; } = 28f;
    public float MarkerLabelScale { get; set; } = 1f;

    // ── Changelog ─────────────────────────────────────────────────────
    public string LastSeenChangelogVersion { get; set; } = "";
    public bool ShowChangelogOnUpdate { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}