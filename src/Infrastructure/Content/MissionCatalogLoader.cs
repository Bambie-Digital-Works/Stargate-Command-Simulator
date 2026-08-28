using System.Text.Json;
using WormholeWorlds.Core.Missions;

namespace WormholeWorlds.Infrastructure.Content;

public sealed class MissionCatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public MissionCatalog Load(string json)
    {
        try
        {
            MissionDocument document = JsonSerializer.Deserialize<MissionDocument>(json, Options)
                ?? throw new InvalidDataException("Mission catalog is empty.");
            if (document.SchemaVersion != 1 || document.Missions is null)
            {
                throw new InvalidDataException("Mission catalog must use schemaVersion 1 and include missions.");
            }

            return new MissionCatalog(document.Missions.Select(item => new MissionDefinition(
                item.Id ?? string.Empty,
                item.DisplayName ?? string.Empty,
                item.Summary ?? string.Empty,
                item.DestinationId ?? string.Empty,
                item.Objective ?? string.Empty,
                item.RequiredSpecialties ?? [],
                item.RiskRating,
                item.FieldDurationMilliseconds,
                item.ContactFaction ?? string.Empty)));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Mission catalog contains an invalid field or value.", exception);
        }
    }

    private sealed record MissionDocument(
        int SchemaVersion,
        IReadOnlyList<MissionEntry>? Missions);

    private sealed record MissionEntry(
        string? Id,
        string? DisplayName,
        string? Summary,
        string? DestinationId,
        string? Objective,
        IReadOnlyList<string>? RequiredSpecialties,
        int RiskRating,
        long FieldDurationMilliseconds,
        string? ContactFaction);
}
