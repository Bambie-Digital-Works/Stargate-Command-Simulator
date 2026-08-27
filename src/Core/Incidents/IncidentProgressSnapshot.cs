namespace WormholeWorlds.Core.Incidents;

public sealed record IncidentProgressSnapshot(
    string? ActiveIncidentId,
    IncidentRunState ActiveState,
    int CompletedCount,
    int FailedCount,
    int TotalCount,
    IReadOnlyList<string> CompletedIncidentIds,
    IReadOnlyList<string> FailedIncidentIds,
    IReadOnlyList<IncidentDebriefFact> DebriefFacts);
