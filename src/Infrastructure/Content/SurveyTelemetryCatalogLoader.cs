using System.Text.Json;
using FacilityCommand.Core.Survey;

namespace FacilityCommand.Infrastructure.Content;

public sealed class SurveyTelemetryCatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public SurveyTelemetryCatalog Load(string json)
    {
        try
        {
            CatalogDocument document = JsonSerializer.Deserialize<CatalogDocument>(json, Options)
                ?? throw new InvalidDataException("Survey Telemetry catalog is empty.");
            if (document.SchemaVersion != 1 || document.Profiles is null)
            {
                throw new InvalidDataException("Survey Telemetry catalog must use schemaVersion 1 and include profiles.");
            }

            return new SurveyTelemetryCatalog(document.Profiles.Select(ParseProfile));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Survey Telemetry catalog contains an invalid field or value.", exception);
        }
    }

    private static SurveyTelemetryProfile ParseProfile(ProfileDocument item)
    {
        if (string.IsNullOrWhiteSpace(item.DestinationId))
        {
            throw new InvalidDataException("Survey Telemetry profile requires destinationId.");
        }

        if (item.DeployDelayMs < 0)
        {
            throw new InvalidDataException($"Survey Telemetry profile '{item.DestinationId}' has a negative deployDelayMs.");
        }

        if (string.IsNullOrWhiteSpace(item.RiskSummary))
        {
            throw new InvalidDataException($"Survey Telemetry profile '{item.DestinationId}' requires riskSummary.");
        }

        if (item.Readings is null || item.Readings.Count == 0)
        {
            throw new InvalidDataException($"Survey Telemetry profile '{item.DestinationId}' requires readings.");
        }

        return new SurveyTelemetryProfile(
            item.DestinationId,
            item.DeployDelayMs,
            ParseSuggestedRisk(item.SuggestedRisk, item.DestinationId),
            item.RiskSummary,
            item.Readings.Select(reading => ParseReading(reading, item.DestinationId)).ToArray());
    }

    private static SurveyChannelReading ParseReading(ReadingDocument reading, string destinationId)
    {
        if (string.IsNullOrWhiteSpace(reading.Label) || string.IsNullOrWhiteSpace(reading.ReportedValue))
        {
            throw new InvalidDataException($"Survey Telemetry profile '{destinationId}' has an incomplete reading.");
        }

        return new SurveyChannelReading(
            ParseChannel(reading.Channel, destinationId),
            reading.Label,
            reading.ReportedValue,
            ParseQuality(reading.Quality, destinationId),
            reading.ContradictionNote);
    }

    private static SurveyChannelKind ParseChannel(string? value, string destinationId) => value switch
    {
        "atmosphere" => SurveyChannelKind.Atmosphere,
        "radiation" => SurveyChannelKind.Radiation,
        "biology" => SurveyChannelKind.Biology,
        "terrain" => SurveyChannelKind.Terrain,
        "camera" => SurveyChannelKind.Camera,
        "signalQuality" => SurveyChannelKind.SignalQuality,
        _ => throw new InvalidDataException($"Survey Telemetry profile '{destinationId}' has an unknown channel '{value}'."),
    };

    private static SurveyReadingQuality ParseQuality(string? value, string destinationId) => value switch
    {
        "clear" => SurveyReadingQuality.Clear,
        "delayed" => SurveyReadingQuality.Delayed,
        "missing" => SurveyReadingQuality.Missing,
        "noisy" => SurveyReadingQuality.Noisy,
        "contradictory" => SurveyReadingQuality.Contradictory,
        _ => throw new InvalidDataException($"Survey Telemetry profile '{destinationId}' has an unknown quality '{value}'."),
    };

    private static SurveyRiskAssessment ParseSuggestedRisk(string? value, string destinationId) => value switch
    {
        "acceptable" => SurveyRiskAssessment.Acceptable,
        "elevated" => SurveyRiskAssessment.Elevated,
        "unacceptable" => SurveyRiskAssessment.Unacceptable,
        _ => throw new InvalidDataException($"Survey Telemetry profile '{destinationId}' has an unknown suggestedRisk '{value}'."),
    };

    private sealed record CatalogDocument(int SchemaVersion, IReadOnlyList<ProfileDocument>? Profiles);

    private sealed record ProfileDocument(
        string? DestinationId,
        int DeployDelayMs,
        string? SuggestedRisk,
        string? RiskSummary,
        IReadOnlyList<ReadingDocument>? Readings);

    private sealed record ReadingDocument(
        string? Channel,
        string? Label,
        string? ReportedValue,
        string? Quality,
        string? ContradictionNote);
}
