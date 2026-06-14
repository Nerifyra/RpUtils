using Dalamud.Configuration;
using System;
using System.Collections.Generic;

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
    public bool RollRequestedSoundAlert { get; set; } = true;
    public uint RollRequestedSoundId { get; set; } = 2;

    public bool RollResultsChatAlert { get; set; } = true;
    public bool RollResultsSoundAlert { get; set; } = true;
    public uint RollResultsSoundId { get; set; } = 2;

    public bool YourTurnChatAlert { get; set; } = true;
    public bool YourTurnSoundAlert { get; set; } = true;
    public uint YourTurnSoundId { get; set; } = 2;

    public bool OnDeckChatAlert { get; set; } = true;
    public bool OnDeckSoundAlert { get; set; } = true;
    public uint OnDeckSoundId { get; set; } = 2;

    // ── Markers ───────────────────────────────────────────────────────
    public float MarkerIconSize { get; set; } = 28f;
    public float MarkerLabelScale { get; set; } = 1f;

    // ── Icon Picker ───────────────────────────────────────────────────
    public List<uint> FavoriteIcons { get; set; } = new();

    // ── Changelog ─────────────────────────────────────────────────────
    public string LastSeenChangelogVersion { get; set; } = "";
    public bool ShowChangelogOnUpdate { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}