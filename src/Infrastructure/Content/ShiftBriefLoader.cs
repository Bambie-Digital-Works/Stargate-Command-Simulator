using System.Text.Json;
using FacilityCommand.Core.Shift;

namespace FacilityCommand.Infrastructure.Content;

public sealed class ShiftBriefLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public ShiftBriefDefinition Load(string json)
    {
        try
        {
            BriefDocument document = JsonSerializer.Deserialize<BriefDocument>(json, Options)
                ?? throw new InvalidDataException("Shift brief is empty.");
            if (document.SchemaVersion != 1)
            {
                throw new InvalidDataException("Shift brief must use schemaVersion 1.");
            }

            if (string.IsNullOrWhiteSpace(document.ShiftName)
                || string.IsNullOrWhiteSpace(document.Title)
                || string.IsNullOrWhiteSpace(document.Summary))
            {
                throw new InvalidDataException("Shift brief requires shiftName, title, and summary.");
            }

            return new ShiftBriefDefinition(
                document.ShiftName,
                document.Title,
                document.Summary,
                document.Objectives ?? [],
                document.KnownRisks ?? [],
                document.ReadinessLines ?? [],
                document.Constraints ?? []);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Shift brief contains an invalid field or value.", exception);
        }
    }

    private sealed record BriefDocument(
        int SchemaVersion,
        string? ShiftName,
        string? Title,
        string? Summary,
        IReadOnlyList<string>? Objectives,
        IReadOnlyList<string>? KnownRisks,
        IReadOnlyList<string>? ReadinessLines,
        IReadOnlyList<string>? Constraints);
}
