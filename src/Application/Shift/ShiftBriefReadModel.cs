using FacilityCommand.Core.Shift;

namespace FacilityCommand.Application.Shift;

public sealed record ShiftBriefReadModel(
    string Title,
    string Summary,
    IReadOnlyList<string> Objectives,
    IReadOnlyList<string> KnownRisks,
    IReadOnlyList<string> ReadinessLines,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<string> CarryoverLines,
    ShiftPhase Phase,
    bool CanStartShift,
    string? PersistenceGuidance);
