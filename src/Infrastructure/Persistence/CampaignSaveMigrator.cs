using System.Text.Json;
using System.Text.Json.Nodes;

namespace FacilityCommand.Infrastructure.Persistence;

public static class CampaignSaveMigrator
{
    public static JsonObject MigrateToCurrent(JsonObject document, int fromVersion, int supportedVersion)
    {
        JsonObject current = document;
        int version = fromVersion;
        while (version < supportedVersion)
        {
            current = version switch
            {
                1 => MigrateV1ToV2(current),
                _ => throw new InvalidDataException($"No campaign save migrator registered for schema {version}."),
            };
            version++;
        }

        return current;
    }

    private static JsonObject MigrateV1ToV2(JsonObject source)
    {
        JsonObject target = JsonNode.Parse(source.ToJsonString())!.AsObject();
        target.Remove("schemaVersion");
        target["saveSchemaVersion"] = 2;
        if (target["writtenAtUtc"] is null)
        {
            target["writtenAtUtc"] = DateTimeOffset.UtcNow.ToString("O");
        }

        return target;
    }

    public static int ReadSchemaVersion(JsonObject document)
    {
        if (document.TryGetPropertyValue("saveSchemaVersion", out JsonNode? saveVersion)
            && saveVersion is JsonValue saveValue
            && saveValue.TryGetValue(out int saveSchema))
        {
            return saveSchema;
        }

        if (document.TryGetPropertyValue("schemaVersion", out JsonNode? legacyVersion)
            && legacyVersion is JsonValue legacyValue
            && legacyValue.TryGetValue(out int schemaVersion))
        {
            return schemaVersion;
        }

        throw new InvalidDataException("Campaign save is missing saveSchemaVersion.");
    }
}
