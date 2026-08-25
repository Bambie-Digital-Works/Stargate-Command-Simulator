using System.Text.Json.Serialization;
using FacilityCommand.Application.Logging;

namespace FacilityCommand.Application.Configuration;

public sealed record AppConfiguration(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("logging")] LoggingOptions Logging,
    [property: JsonPropertyName("diagnostics")] DiagnosticsOptions Diagnostics,
    [property: JsonPropertyName("simulation")] SimulationOptions Simulation);

public sealed record LoggingOptions(
    [property: JsonPropertyName("minimumLevel")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<ApplicationLogLevel>))]
    ApplicationLogLevel MinimumLevel,
    [property: JsonPropertyName("maxFiles")] int MaxFiles,
    [property: JsonPropertyName("maxFileBytes")] long MaxFileBytes);

public sealed record DiagnosticsOptions(
    [property: JsonPropertyName("overlayVisibleOnStartup")] bool OverlayVisibleOnStartup,
    [property: JsonPropertyName("recentLogEntries")] int RecentLogEntries);

public sealed record SimulationOptions(
    [property: JsonPropertyName("preparationTimeoutMs")] int PreparationTimeoutMs,
    [property: JsonPropertyName("vectorLockTimeoutMs")] int VectorLockTimeoutMs,
    [property: JsonPropertyName("stabilizationTimeoutMs")] int StabilizationTimeoutMs,
    [property: JsonPropertyName("closingTimeoutMs")] int ClosingTimeoutMs,
    [property: JsonPropertyName("cooldownTimeoutMs")] int CooldownTimeoutMs,
    [property: JsonPropertyName("availablePowerUnits")] int AvailablePowerUnits,
    [property: JsonPropertyName("availableCoolingUnits")] int AvailableCoolingUnits);

public sealed record AppConfigurationOverride(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("logging")] LoggingOptionsOverride? Logging,
    [property: JsonPropertyName("diagnostics")] DiagnosticsOptionsOverride? Diagnostics,
    [property: JsonPropertyName("simulation")] SimulationOptionsOverride? Simulation);

public sealed record LoggingOptionsOverride(
    [property: JsonPropertyName("minimumLevel")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<ApplicationLogLevel>))]
    ApplicationLogLevel? MinimumLevel,
    [property: JsonPropertyName("maxFiles")] int? MaxFiles,
    [property: JsonPropertyName("maxFileBytes")] long? MaxFileBytes);

public sealed record DiagnosticsOptionsOverride(
    [property: JsonPropertyName("overlayVisibleOnStartup")] bool? OverlayVisibleOnStartup,
    [property: JsonPropertyName("recentLogEntries")] int? RecentLogEntries);

public sealed record SimulationOptionsOverride(
    [property: JsonPropertyName("preparationTimeoutMs")] int? PreparationTimeoutMs,
    [property: JsonPropertyName("vectorLockTimeoutMs")] int? VectorLockTimeoutMs,
    [property: JsonPropertyName("stabilizationTimeoutMs")] int? StabilizationTimeoutMs,
    [property: JsonPropertyName("closingTimeoutMs")] int? ClosingTimeoutMs,
    [property: JsonPropertyName("cooldownTimeoutMs")] int? CooldownTimeoutMs,
    [property: JsonPropertyName("availablePowerUnits")] int? AvailablePowerUnits,
    [property: JsonPropertyName("availableCoolingUnits")] int? AvailableCoolingUnits);
