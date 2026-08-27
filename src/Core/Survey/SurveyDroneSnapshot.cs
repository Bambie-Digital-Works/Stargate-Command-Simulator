using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Survey;

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
