namespace FacilityCommand.Core.Incidents;

public sealed record IncidentDefinition(
    string Id,
    string DisplayName,
    int SequenceOrder,
    string ObjectiveSummary,
    string SuccessEvent,
    string FailureEvent,
    IReadOnlyList<string> SuccessFacts,
    IReadOnlyList<string> FailureFacts,
    string? ActivationHint);
