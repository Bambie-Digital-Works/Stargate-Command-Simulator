namespace FacilityCommand.Application.Replay;

public sealed record ReplayDocument(
    int SchemaVersion,
    int ContentSchemaVersion,
    ulong Seed,
    IReadOnlyList<ReplayRecord> Records,
    string Sha256);
