using System.Text.Json;
using WormholeWorlds.Application.Accessibility;
using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Infrastructure.Accessibility;

public sealed record AccessibilityLoadResult(
    AccessibilityPreferences Preferences,
    string? RecoveryWarning);

public sealed class AccessibilitySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    private readonly string _path;

    public AccessibilitySettingsStore(string path)
    {
        _path = path;
    }

    public AccessibilityLoadResult LoadOrDefault()
    {
        if (!File.Exists(_path))
        {
            return new AccessibilityLoadResult(AccessibilityPreferences.Default, null);
        }

        try
        {
            AccessibilityPreferences? preferences = JsonSerializer.Deserialize<AccessibilityPreferences>(
                File.ReadAllText(_path),
                JsonOptions);
            if (preferences is null || preferences.Validate().Count > 0)
            {
                return Recover("Accessibility settings were invalid and defaults were restored.");
            }

            return new AccessibilityLoadResult(preferences, null);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return Recover("Accessibility settings could not be read and defaults were restored.");
        }
    }

    public void Save(AccessibilityPreferences preferences)
    {
        IReadOnlyList<string> errors = preferences.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Cannot save accessibility settings: {errors[0]}");
        }

        string json = JsonSerializer.Serialize(preferences, JsonOptions);
        AtomicJsonFileStore.WriteAtomically(
            _path,
            json,
            candidate =>
            {
                AccessibilityPreferences? parsed = JsonSerializer.Deserialize<AccessibilityPreferences>(candidate, JsonOptions);
                return parsed is not null && parsed.Validate().Count == 0;
            });
    }

    private AccessibilityLoadResult Recover(string warning)
    {
        AtomicJsonFileStore.Quarantine(_path, "corrupt");
        return new AccessibilityLoadResult(AccessibilityPreferences.Default, warning);
    }
}
