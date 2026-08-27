namespace WormholeWorlds.Core.Survey;

public sealed record SurveyTelemetryProfile(
    string DestinationId,
    int DeployDelayMilliseconds,
    SurveyRiskAssessment SuggestedRisk,
    string RiskSummary,
    IReadOnlyList<SurveyChannelReading> Readings);
