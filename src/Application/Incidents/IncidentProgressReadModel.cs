using FacilityCommand.Core.Incidents;

namespace FacilityCommand.Application.Incidents;

public sealed record IncidentProgressReadModel(
    string? ActiveIncidentId,
    string ActiveIncidentTitle,
    string ObjectiveSummary,
    string ActivationHint,
    string PhaseLabel,
    int CompletedCount,
    int FailedCount,
    int TotalCount,
    bool SequenceComplete,
    IReadOnlyList<string> DebriefFactSummaries);
