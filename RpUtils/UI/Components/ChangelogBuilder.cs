using System;
using System.Collections.Generic;

namespace RpUtils.UI.Components;

// ── Data Model ───────────────────────────────────────────────────────────────

internal enum ChangeEntryType { Breaking, Feature, Fix }

internal record ChangeEntry(ChangeEntryType Type, string Text, List<string> Details);

internal record ChangeVersion(string Version, List<ChangeEntry> Entries);

// ── Builder ──────────────────────────────────────────────────────────────────

/// <summary>
/// Fluent builder for constructing a <see cref="Changelog"/>.
/// Versions are displayed in the order they are added (newest first recommended).
/// </summary>
public sealed class ChangelogBuilder
{
    private readonly List<ChangeVersion> _versions = [];
    private ChangeVersion? _current;

    /// <summary>
    /// Starts a new version section. All subsequent entries belong to this version
    /// until the next <see cref="NextVersion"/> call.
    /// </summary>
    public ChangelogBuilder NextVersion(string version)
    {
        _current = new ChangeVersion(version, []);
        _versions.Add(_current);
        return this;
    }

    /// <summary>Adds a breaking change entry (displayed in red).</summary>
    public ChangelogBuilder Critical(string text)
    {
        AddEntry(ChangeEntryType.Breaking, text);
        return this;
    }

    /// <summary>Adds a feature entry (displayed in purple).</summary>
    public ChangelogBuilder Important(string text)
    {
        AddEntry(ChangeEntryType.Feature, text);
        return this;
    }

    /// <summary>Adds a bug fix entry (displayed in white).</summary>
    public ChangelogBuilder Minor(string text)
    {
        AddEntry(ChangeEntryType.Fix, text);
        return this;
    }

    /// <summary>Adds an indented detail line under the most recent entry.</summary>
    public ChangelogBuilder Detail(string text)
    {
        if (_current == null || _current.Entries.Count == 0)
            throw new InvalidOperationException("Detail() must be called after a Critical(), Important(), or Minor() entry.");
        _current.Entries[^1].Details.Add(text);
        return this;
    }

    /// <summary>Builds the <see cref="Changelog"/>.</summary>
    public Changelog Build() => new(_versions);

    private void AddEntry(ChangeEntryType type, string text)
    {
        if (_current == null)
            throw new InvalidOperationException("NextVersion() must be called before adding entries.");
        _current.Entries.Add(new ChangeEntry(type, text, []));
    }
}
