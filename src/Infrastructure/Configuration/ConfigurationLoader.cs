using System.Text.Json;
using FacilityCommand.Application.Configuration;

namespace FacilityCommand.Infrastructure.Configuration;

public sealed class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public AppConfiguration Load(string defaultsJson, string? overrideJson = null)
    {
        AppConfiguration defaults = Deserialize<AppConfiguration>(defaultsJson, "built-in configuration");
        Validate(defaults, "built-in configuration");

        if (string.IsNullOrWhiteSpace(overrideJson))
        {
            return defaults;
        }

        AppConfigurationOverride userOverride = Deserialize<AppConfigurationOverride>(overrideJson, "user configuration");
        if (userOverride.SchemaVersion != 1)
        {
            throw new ConfigurationException("user configuration", ["schemaVersion must be 1."]);
        }

        AppConfiguration merged = new(
            defaults.SchemaVersion,
            new LoggingOptions(
                userOverride.Logging?.MinimumLevel ?? defaults.Logging.MinimumLevel,
                userOverride.Logging?.MaxFiles ?? defaults.Logging.MaxFiles,
                userOverride.Logging?.MaxFileBytes ?? defaults.Logging.MaxFileBytes),
            new DiagnosticsOptions(
                userOverride.Diagnostics?.OverlayVisibleOnStartup ?? defaults.Diagnostics.OverlayVisibleOnStartup,
                userOverride.Diagnostics?.RecentLogEntries ?? defaults.Diagnostics.RecentLogEntries));

        Validate(merged, "user configuration");
        return merged;
    }

    private static T Deserialize<T>(string json, string sourceLabel)
    {
        try
        {
            T? result = JsonSerializer.Deserialize<T>(json, JsonOptions);
            return result ?? throw new ConfigurationException(sourceLabel, ["document is empty."]);
        }
        catch (JsonException exception)
        {
            string field = string.IsNullOrWhiteSpace(exception.Path) ? "document" : exception.Path;
            throw new ConfigurationException(sourceLabel, [$"{field}: invalid value or structure."]);
        }
    }

    private static void Validate(AppConfiguration configuration, string sourceLabel)
    {
        List<string> errors = [];
        if (configuration.SchemaVersion != 1)
        {
            errors.Add("schemaVersion must be 1.");
        }

        if (configuration.Logging is null)
        {
            errors.Add("logging is required.");
        }
        else
        {
            if (!Enum.IsDefined(configuration.Logging.MinimumLevel))
            {
                errors.Add("logging.minimumLevel is unsupported.");
            }

            if (configuration.Logging.MaxFiles is < 1 or > 20)
            {
                errors.Add("logging.maxFiles must be between 1 and 20.");
            }

            if (configuration.Logging.MaxFileBytes is < 65536 or > 16777216)
            {
                errors.Add("logging.maxFileBytes must be between 65536 and 16777216.");
            }
        }

        if (configuration.Diagnostics is null)
        {
            errors.Add("diagnostics is required.");
        }
        else if (configuration.Diagnostics.RecentLogEntries is < 1 or > 100)
        {
            errors.Add("diagnostics.recentLogEntries must be between 1 and 100.");
        }

        if (errors.Count > 0)
        {
            throw new ConfigurationException(sourceLabel, errors);
        }
    }
}

