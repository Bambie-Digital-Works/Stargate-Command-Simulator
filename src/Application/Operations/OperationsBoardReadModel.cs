using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Operations;

public sealed record OperationsBoardReadModel(
    long MissionClockMilliseconds,
    string MissionClockDisplay,
    TransitArrayPhase TransitPhase,
    string TransitPhaseLabel,
    string DestinationSummary,
    ContainmentShutterState ShutterState,
    string ContainmentSummary,
    string CredentialSummary,
    int PowerCapacity,
    int CoolingCapacity,
    int ReservedPower,
    int ReservedCooling,
    int FreePower,
    int FreeCooling,
    string ExpeditionUnitSummary,
    string SurveyTelemetrySummary,
    string IncidentSummary,
    string IncidentObjective,
    string IncidentGuidance,
    IReadOnlyList<OperationsAlarm> ActiveAlarms,
    string AnnouncementSummary,
    OperatorConsoleScreen ActiveScreen,
    bool CanEndShift);
