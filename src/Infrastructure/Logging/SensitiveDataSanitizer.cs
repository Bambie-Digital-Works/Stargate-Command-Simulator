using System.Text.RegularExpressions;

namespace WormholeWorlds.Infrastructure.Logging;

public sealed partial class SensitiveDataSanitizer
{
    private static readonly HashSet<string> AllowedPropertyNames = new(StringComparer.Ordinal)
    {
        "action", "buildId", "channel", "component", "device", "reason", "schema", "state", "version",
    };

    private readonly string _userName;

    public SensitiveDataSanitizer(string? userName = null)
    {
        _userName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
    }

    public string Sanitize(string value)
    {
        string sanitized = CredentialPattern().Replace(value, "$1=<redacted>");
        sanitized = WindowsPathPattern().Replace(sanitized, "<path>");
        sanitized = UnixUserPathPattern().Replace(sanitized, "<path>");
        if (!string.IsNullOrWhiteSpace(_userName))
        {
            sanitized = sanitized.Replace(_userName, "<user>", StringComparison.OrdinalIgnoreCase);
        }

        return sanitized;
    }

    public IReadOnlyDictionary<string, string> SanitizeProperties(IReadOnlyDictionary<string, string>? properties)
    {
        if (properties is null)
        {
            return new Dictionary<string, string>();
        }

        return properties.ToDictionary(
            pair => pair.Key,
            pair => AllowedPropertyNames.Contains(pair.Key) ? Sanitize(pair.Value) : "<redacted>",
            StringComparer.Ordinal);
    }

    [GeneratedRegex(@"(?i)\b(password|passwd|token|secret|authorization|credential)\s*[:=]\s*[^\s,;]+")]
    private static partial Regex CredentialPattern();

    [GeneratedRegex(@"(?i)\b[A-Z]:\\(?:[^\s\\/:*?\""<>|]+\\)*[^\s\\/:*?\""<>|]*")]
    private static partial Regex WindowsPathPattern();

    [GeneratedRegex(@"(?i)/(?:home|users)/[^\s/]+(?:/[^\s]*)?")]
    private static partial Regex UnixUserPathPattern();
}

