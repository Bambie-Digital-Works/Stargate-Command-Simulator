using FacilityCommand.Core.Survey;

namespace FacilityCommand.Application.Survey;

public sealed record SurveyChannelView(
    string ChannelLabel,
    string ReportedValue,
    string QualityLabel,
    string? ContradictionNote);

public sealed record SurveyTelemetryReadModel(
    string DeployStateLabel,
    string? DestinationId,
    string StatusSummary,
    string RiskSummary,
    string SuggestedRiskLabel,
    string RecordedDecisionLabel,
    IReadOnlyList<SurveyChannelView> Channels,
    bool HasContradictoryReadings,
    bool CanDeploy,
    bool CanResolveTelemetry,
    bool CanRecordDecision,
    bool HasRecordedDecision,
    bool SurveyKitAvailable,
    bool LinkIsStable);
