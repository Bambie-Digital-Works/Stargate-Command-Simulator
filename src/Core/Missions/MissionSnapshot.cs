namespace WormholeWorlds.Core.Missions;

public sealed record MissionSnapshot(
    string MissionId,
    MissionPhase Phase,
    string? AssignedUnitId,
    long StartedAtMilliseconds,
    long? CompletedAtMilliseconds,
    int ProgressPercent,
    string? OutcomeSummary);
