using System.Reflection;

namespace RpUtils;

public static class PluginConstants
{
    public static string PluginVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    public const string ApiVersion = "0.2.0";
    public const string DiscordInviteUrl = "https://discord.gg/TguVmQuQZz";

    /// <summary>
    /// Extracts the "major.minor" portion of a semver string.
    /// </summary>
    public static string GetReleaseVersion(string version)
    {
        var parts = version.Split('.');
        return parts.Length >= 2 ? $"{parts[0]}.{parts[1]}" : version;
    }
    public const string HubAddress = "/rpUtilsHub";

    #if DEBUG
        public const string ServerAddress = "http://localhost:8080";
    #else
        public const string ServerAddress = "http://rputils.catwitch.dev:8080";
    #endif
}
