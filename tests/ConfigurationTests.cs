using FacilityCommand.Application.Configuration;
using FacilityCommand.Application.Logging;
using FacilityCommand.Infrastructure.Configuration;

namespace FacilityCommand.Tests;

public sealed class ConfigurationTests
{
    private const string ValidDefaults = """
        {
          "schemaVersion": 1,
          "logging": {
            "minimumLevel": "information",
            "maxFiles": 5,
            "maxFileBytes": 1048576
          },
          "diagnostics": {
            "overlayVisibleOnStartup": false,
            "recentLogEntries": 20
          }
        }
        """;

    [Fact]
    public void LoadsDefaultsAndPartialOverride()
    {
        const string userOverride = """
            {
              "schemaVersion": 1,
              "logging": { "minimumLevel": "warning" },
              "diagnostics": { "overlayVisibleOnStartup": true }
            }
            """;

        AppConfiguration result = new ConfigurationLoader().Load(ValidDefaults, userOverride);

        Assert.Equal(ApplicationLogLevel.Warning, result.Logging.MinimumLevel);
        Assert.Equal(5, result.Logging.MaxFiles);
        Assert.True(result.Diagnostics.OverlayVisibleOnStartup);
        Assert.Equal(20, result.Diagnostics.RecentLogEntries);
    }

    [Fact]
    public void RejectsUnknownFieldsWithSafeSourceAndField()
    {
        string invalid = ValidDefaults.Replace("\"recentLogEntries\": 20", "\"recentLogEntries\": 20, \"surprise\": true");

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new ConfigurationLoader().Load(invalid));

        Assert.Equal("built-in configuration", exception.SourceLabel);
        Assert.Contains("surprise", exception.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Environment.UserName, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsInvalidRanges()
    {
        string invalid = ValidDefaults.Replace("\"maxFiles\": 5", "\"maxFiles\": 0");

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new ConfigurationLoader().Load(invalid));

        Assert.Contains("logging.maxFiles", exception.Message, StringComparison.Ordinal);
    }
}
