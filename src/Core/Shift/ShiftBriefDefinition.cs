namespace WormholeWorlds.Core.Shift;

public sealed record ShiftBriefDefinition(
    string ShiftName,
    string Title,
    string Summary,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> KnownRisks,
    IReadOnlyList<string> ReadinessLines,
    IReadOnlyList<string> Constraints);
