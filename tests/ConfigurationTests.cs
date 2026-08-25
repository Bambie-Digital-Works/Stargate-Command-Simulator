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
          },
          "simulation": {
            "preparationTimeoutMs": 10000,
            "vectorLockTimeoutMs": 5000,
            "stabilizationTimeoutMs": 8000,
            "closingTimeoutMs": 5000,
            "cooldownTimeoutMs": 3000,
            "availablePowerUnits": 100,
            "availableCoolingUnits": 100
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
        Assert.Equal(100, result.Simulation.AvailablePowerUnits);
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

    [Fact]
    public void RejectsInvalidSimulationCapacity()
    {
        string invalid = ValidDefaults.Replace("\"availablePowerUnits\": 100", "\"availablePowerUnits\": 0");

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => new ConfigurationLoader().Load(invalid));

        Assert.Contains("simulation.availablePowerUnits", exception.Message, StringComparison.Ordinal);
    }
}
