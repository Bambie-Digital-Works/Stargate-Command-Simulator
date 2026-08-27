using System.Text.Json;
using WormholeWorlds.Core.Incidents;

namespace WormholeWorlds.Infrastructure.Content;

public sealed class IncidentCatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public IncidentCatalog Load(string json)
    {
        try
        {
            CatalogDocument document = JsonSerializer.Deserialize<CatalogDocument>(json, Options)
                ?? throw new InvalidDataException("Incident catalog is empty.");
            if (document.SchemaVersion != 1 || document.Incidents is null)
            {
                throw new InvalidDataException("Incident catalog must use schemaVersion 1 and include incidents.");
            }

            return new IncidentCatalog(document.Incidents.Select(Parse));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Incident catalog contains an invalid field or value.", exception);
        }
    }

    private static IncidentDefinition Parse(IncidentDocument item)
    {
        if (string.IsNullOrWhiteSpace(item.Id)
            || string.IsNullOrWhiteSpace(item.DisplayName)
            || string.IsNullOrWhiteSpace(item.ObjectiveSummary)
            || string.IsNullOrWhiteSpace(item.SuccessEvent)
            || string.IsNullOrWhiteSpace(item.FailureEvent))
        {
            throw new InvalidDataException("Incident definitions require id, displayName, objectiveSummary, successEvent, and failureEvent.");
        }

        if (item.SequenceOrder < 1)
        {
            throw new InvalidDataException($"Incident '{item.Id}' requires a positive sequenceOrder.");
        }

        return new IncidentDefinition(
            item.Id,
            item.DisplayName,
            item.SequenceOrder,
            item.ObjectiveSummary,
            item.SuccessEvent,
            item.FailureEvent,
            item.SuccessFacts ?? [],
            item.FailureFacts ?? [],
            item.ActivationHint);
    }

    private sealed record CatalogDocument(int SchemaVersion, IReadOnlyList<IncidentDocument>? Incidents);

    private sealed record IncidentDocument(
        string? Id,
        string? DisplayName,
        int SequenceOrder,
        string? ObjectiveSummary,
        string? SuccessEvent,
        string? FailureEvent,
        IReadOnlyList<string>? SuccessFacts,
        IReadOnlyList<string>? FailureFacts,
        string? ActivationHint);
}
