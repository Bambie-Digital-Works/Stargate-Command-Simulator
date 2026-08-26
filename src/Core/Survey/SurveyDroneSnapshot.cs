using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Survey;

public sealed record SurveyDroneSnapshot(
    SurveyDeployState State,
    string? DestinationId,
    SimulationInstant? DeployedAt,
    int DeployDelayMilliseconds,
    IReadOnlyList<SurveyChannelReading> Readings,
    SurveyRiskAssessment? SuggestedRisk,
    string? RiskSummary,
    SurveyRiskAssessment? RecordedDecision,
    bool HasContradictoryReadings);
