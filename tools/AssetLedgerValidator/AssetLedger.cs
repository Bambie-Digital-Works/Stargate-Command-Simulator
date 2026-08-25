using System.Text.Json.Serialization;

namespace FacilityCommand.Tools.AssetLedgerValidator;

public sealed record AssetLedger(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("assets")] IReadOnlyList<AssetRecord>? Assets);

public sealed record AssetRecord(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("path")] string? Path,
    [property: JsonPropertyName("category")] string? Category,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("origin")] string? Origin,
    [property: JsonPropertyName("creator")] string? Creator,
    [property: JsonPropertyName("sourceUrl")] string? SourceUrl,
    [property: JsonPropertyName("licenseExpression")] string? LicenseExpression,
    [property: JsonPropertyName("acquiredOn")] string? AcquiredOn,
    [property: JsonPropertyName("modifications")] string? Modifications,
    [property: JsonPropertyName("attribution")] string? Attribution,
    [property: JsonPropertyName("proofLocation")] string? ProofLocation,
    [property: JsonPropertyName("reviewStatus")] string? ReviewStatus,
    [property: JsonPropertyName("reviewedBy")] string? ReviewedBy,
    [property: JsonPropertyName("reviewedOn")] string? ReviewedOn,
    [property: JsonPropertyName("distributionAllowed")] bool DistributionAllowed);

