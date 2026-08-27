using System.Text.Json;
using WormholeWorlds.Core.Destinations;

namespace WormholeWorlds.Infrastructure.Content;

public sealed class DestinationRegistryLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public DestinationRegistry Load(string json)
    {
        try
        {
            RegistryDocument document = JsonSerializer.Deserialize<RegistryDocument>(json, Options)
                ?? throw new InvalidDataException("Destination Registry is empty.");
            if (document.SchemaVersion != 1 || document.Destinations is null)
            {
                throw new InvalidDataException("Destination Registry must use schemaVersion 1 and include destinations.");
            }

            return new DestinationRegistry(document.Destinations.Select(item => new DestinationRecord(
                item.Id ?? string.Empty,
                item.DisplayName ?? string.Empty,
                item.Vector ?? [],
                item.RequiredPowerUnits,
                item.RequiredCoolingUnits)));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Destination Registry contains an invalid field or value.", exception);
        }
    }

    private sealed record RegistryDocument(int SchemaVersion, IReadOnlyList<DestinationDocument>? Destinations);

    private sealed record DestinationDocument(
        string? Id,
        string? DisplayName,
        IReadOnlyList<string>? Vector,
        int RequiredPowerUnits,
        int RequiredCoolingUnits);
}
