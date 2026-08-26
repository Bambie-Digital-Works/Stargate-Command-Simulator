using FacilityCommand.Application.Incidents;
using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Simulation;
using FacilityCommand.Application.Survey;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Incidents;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Operations;

public sealed class OperationsBoardService
{
    private readonly TransitSimulationService _transit;
    private readonly ReturnSecurityService _security;
    private readonly ExpeditionRosterService _roster;
    private readonly SurveyTelemetryService _survey;
    private readonly ShiftIncidentService _incidents;
    private readonly ISimulationClock _clock;
    private readonly FacilityResourcePool _resources;
    private OperatorConsoleScreen _activeScreen = OperatorConsoleScreen.OperationsBoard;

    public OperationsBoardService(
        TransitSimulationService transit,
        ReturnSecurityService security,
        ExpeditionRosterService roster,
        SurveyTelemetryService survey,
        ShiftIncidentService incidents,
        ISimulationClock clock,
        FacilityResourcePool resources)
    {
        _transit = transit;
        _security = security;
        _roster = roster;
        _survey = survey;
        _incidents = incidents;
        _clock = clock;
        _resources = resources;
    }

    public OperatorConsoleScreen ActiveScreen => _activeScreen;

    public TransitSimulationService Transit => _transit;

    public ReturnSecurityService Security => _security;

    public ExpeditionRosterService Roster => _roster;

    public SurveyTelemetryService Survey => _survey;

    public ShiftIncidentService Incidents => _incidents;

    public ISimulationClock Clock => _clock;

    public IReadOnlyList<OperatorConsoleScreen> NavigationOrder { get; } =
    [
        OperatorConsoleScreen.OperationsBoard,
        OperatorConsoleScreen.TransitControl,
        OperatorConsoleScreen.SurveyTelemetry,
        OperatorConsoleScreen.ReturnControl,
        OperatorConsoleScreen.ExpeditionRoster,
    ];

    public void SetActiveScreen(OperatorConsoleScreen screen)
    {
        if (!NavigationOrder.Contains(screen))
        {
            throw new ArgumentOutOfRangeException(nameof(screen), screen, "Unknown operator console screen.");
        }

        _activeScreen = screen;
    }

    public OperatorConsoleScreen CycleScreen(int direction)
    {
        int index = NavigationOrder.ToList().IndexOf(_activeScreen);
        int next = (index + Math.Sign(direction) + NavigationOrder.Count) % NavigationOrder.Count;
        _activeScreen = NavigationOrder[next];
        return _activeScreen;
    }

    public OperationsBoardReadModel GetReadModel()
    {
        TransitControlReadModel transit = _transit.GetTransitControl();
        TransitArraySnapshot transitSnapshot = _transit.GetTransitArraySnapshot();
        ReturnSecurityReadModel security = _security.GetReadModel(transitSnapshot);
        ExpeditionRosterReadModel roster = _roster.GetReadModel(transitSnapshot);
        SurveyTelemetryReadModel survey = _survey.GetReadModel();
        IncidentProgressReadModel incidents = _incidents.GetReadModel();
        List<OperationsAlarm> alarms = BuildAlarms(transit.Phase, security, survey, incidents);
        string announcement = BuildAnnouncement(transit, security, survey, incidents, alarms);

        return new OperationsBoardReadModel(
            _clock.Current.Milliseconds,
            FormatMissionClock(_clock.Current.Milliseconds),
            transit.Phase,
            FormatTransitPhase(transit.Phase),
            FormatDestination(transit.DestinationId),
            security.ShutterState,
            FormatContainment(security.ShutterState),
            FormatCredential(security),
            _resources.PowerCapacity,
            _resources.CoolingCapacity,
            transit.ReservedPower,
            transit.ReservedCooling,
            _resources.FreePower,
            _resources.FreeCooling,
            roster.Summary,
            FormatSurveySummary(survey),
            FormatIncidentSummary(incidents),
            incidents.ObjectiveSummary,
            alarms,
            announcement,
            _activeScreen);
    }

    public IncidentOperationResult ResolveCoolingFault() => _incidents.ResolveCoolingFault();

    private static List<OperationsAlarm> BuildAlarms(
        TransitArrayPhase phase,
        ReturnSecurityReadModel security,
        SurveyTelemetryReadModel survey,
        IncidentProgressReadModel incidents)
    {
        List<OperationsAlarm> alarms = [];
        if (phase == TransitArrayPhase.Faulted)
        {
            alarms.Add(new OperationsAlarm(
                "transit_faulted",
                "Alert",
                "Transit Array is faulted and requires recovery."));
        }

        if (phase == TransitArrayPhase.IncomingDetected)
        {
            alarms.Add(new OperationsAlarm(
                "incoming_detected",
                "Warning",
                "Unscheduled incoming Transit Link detected."));
        }

        if (security.ShutterState == ContainmentShutterState.Faulted)
        {
            alarms.Add(new OperationsAlarm(
                "containment_faulted",
                "Alert",
                "Containment Shutter is faulted and secured."));
        }

        if (security.HasSecurityAlert)
        {
            alarms.Add(new OperationsAlarm(
                security.StatusCode,
                "Alert",
                security.CorrectiveAction));
        }

        if (survey.HasContradictoryReadings && !survey.HasRecordedDecision)
        {
            alarms.Add(new OperationsAlarm(
                "survey_contradiction",
                "Warning",
                "Survey Telemetry contains contradictory readings awaiting a risk decision."));
        }

        if (!string.IsNullOrWhiteSpace(incidents.ActiveIncidentId) && !incidents.SequenceComplete)
        {
            alarms.Add(new OperationsAlarm(
                $"incident_{incidents.ActiveIncidentId}",
                "Warning",
                $"{incidents.ActiveIncidentTitle}: {incidents.ObjectiveSummary}"));
        }

        return alarms;
    }

    private static string BuildAnnouncement(
        TransitControlReadModel transit,
        ReturnSecurityReadModel security,
        SurveyTelemetryReadModel survey,
        IncidentProgressReadModel incidents,
        IReadOnlyList<OperationsAlarm> alarms)
    {
        if (alarms.Count > 0)
        {
            OperationsAlarm primary = alarms[0];
            return $"{primary.SeverityLabel}: {primary.Message}";
        }

        if (incidents.SequenceComplete)
        {
            return $"Shift incidents complete ({incidents.CompletedCount} resolved, {incidents.FailedCount} failed).";
        }

        return
            $"Status nominal. Transit Array {FormatTransitPhase(transit.Phase)}. " +
            $"Containment {FormatContainment(security.ShutterState)}. Survey {survey.DeployStateLabel}.";
    }

    private static string FormatIncidentSummary(IncidentProgressReadModel incidents)
    {
        if (incidents.SequenceComplete)
        {
            return $"Incidents complete ({incidents.CompletedCount}/{incidents.TotalCount})";
        }

        return $"Incident {incidents.CompletedCount + incidents.FailedCount + 1}/{incidents.TotalCount}: {incidents.ActiveIncidentTitle}";
    }

    private static string FormatSurveySummary(SurveyTelemetryReadModel survey)
    {
        if (survey.HasRecordedDecision)
        {
            return $"Survey {survey.DeployStateLabel} — decision {survey.RecordedDecisionLabel}";
        }

        if (survey.HasContradictoryReadings)
        {
            return $"Survey {survey.DeployStateLabel} — contradictory readings";
        }

        return $"Survey {survey.DeployStateLabel}";
    }

    private static string FormatMissionClock(long milliseconds)
    {
        long totalSeconds = Math.Max(0, milliseconds) / 1000;
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;
        return hours > 0
            ? $"T+{hours}:{minutes:00}:{seconds:00}"
            : $"T+{minutes:00}:{seconds:00}";
    }

    private static string FormatTransitPhase(TransitArrayPhase phase) => phase switch
    {
        TransitArrayPhase.Standby => "Standby",
        TransitArrayPhase.OutgoingPreparation => "Outgoing preparation",
        TransitArrayPhase.IncomingDetected => "Incoming detected",
        TransitArrayPhase.Sequencing => "Sequencing",
        TransitArrayPhase.Stabilizing => "Stabilizing",
        TransitArrayPhase.LinkOpen => "Link open",
        TransitArrayPhase.Closing => "Closing",
        TransitArrayPhase.Cooldown => "Cooldown",
        TransitArrayPhase.Recovering => "Recovering",
        TransitArrayPhase.Faulted => "Faulted",
        _ => phase.ToString(),
    };

    private static string FormatContainment(ContainmentShutterState state) => state switch
    {
        ContainmentShutterState.Closed => "Closed / secured",
        ContainmentShutterState.Opening => "Opening",
        ContainmentShutterState.Open => "Open",
        ContainmentShutterState.Closing => "Closing",
        ContainmentShutterState.Faulted => "Faulted / secured",
        _ => state.ToString(),
    };

    private static string FormatCredential(ReturnSecurityReadModel security)
    {
        if (security.CredentialStatus is null)
        {
            return "Return Credential not checked.";
        }

        return $"{security.CredentialStatus}: {security.CorrectiveAction}";
    }

    private static string FormatDestination(string? destinationId) =>
        string.IsNullOrWhiteSpace(destinationId) ? "No destination selected." : destinationId;
}
