using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Survey;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Survey;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Content;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class OperationsBoardTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    [Fact]
    public void ReadModelExposesCriticalStateWithoutColourOnlyCues()
    {
        OperationsBoardService service = CreateService(out _, out _, out ManualSimulationClock clock);
        clock.Advance(125_000);

        OperationsBoardReadModel model = service.GetReadModel();

        Assert.Equal("T+02:05", model.MissionClockDisplay);
        Assert.Equal(TransitArrayPhase.Standby, model.TransitPhase);
        Assert.Equal("Standby", model.TransitPhaseLabel);
        Assert.Equal(ContainmentShutterState.Closed, model.ShutterState);
        Assert.Contains("secured", model.ContainmentSummary, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100, model.FreePower);
        Assert.Equal(100, model.FreeCooling);
        Assert.Equal("No Expedition Unit assigned.", model.ExpeditionUnitSummary);
        Assert.Contains("Survey", model.SurveyTelemetrySummary, StringComparison.Ordinal);
        Assert.Contains("Incident", model.IncidentSummary, StringComparison.Ordinal);
        Assert.Contains(model.ActiveAlarms, alarm => alarm.Code.StartsWith("incident_", StringComparison.Ordinal));
    }

    [Fact]
    public void SecurityAlertAndFaultProduceTextAlarms()
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = CreateConnection(resources);
        DestinationRegistry registry = new([Destination]);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryService survey = CreateSurvey(roster, transit, clock);
        roster.BindSurvey(survey);
        ShiftIncidentService incidents = CreateIncidents(transit, security, roster, survey, resources);
        OperationsBoardService board = new(transit, security, roster, survey, incidents, clock, resources);

        Assert.True(outgoing.ReportFault(new SimulationInstant(1), "array_fault").IsAccepted);
        security.VerifyCredential(
            new ReturnCredential(
                "credential-7",
                "expedition-unit-7",
                "proof-7",
                new SimulationInstant(100),
                ReturnCredentialStatus.Duress),
            "proof-7",
            new SimulationInstant(10));

        OperationsBoardReadModel model = board.GetReadModel();

        Assert.Contains(model.ActiveAlarms, alarm => alarm.Code == "transit_faulted" && alarm.SeverityLabel == "Alert");
        Assert.Contains(model.ActiveAlarms, alarm => alarm.Code == "credential_duress" && alarm.SeverityLabel == "Alert");
        Assert.StartsWith("Alert:", model.AnnouncementSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void RepeatedRefreshDoesNotDuplicateStillActiveAlarmsInSystemsHistory()
    {
        OperationsBoardService service = CreateService(out TransitSimulationService transit, out _, out _);
        Assert.True(transit.ReportFault(new SimulationInstant(1), "array_fault").IsAccepted);

        service.GetReadModel();
        IReadOnlyList<string> firstHistory = service.Systems.GetReadModel().AlarmHistory;
        service.GetReadModel();
        IReadOnlyList<string> secondHistory = service.Systems.GetReadModel().AlarmHistory;

        Assert.NotEmpty(firstHistory);
        Assert.Equal(firstHistory, secondHistory);
    }

    [Fact]
    public void NavigationCyclesThroughPrototypeWorkflows()
    {
        OperationsBoardService service = CreateService(out _, out _, out _);
        service.SetActiveScreen(OperatorConsoleScreen.OperationsBoard);

        Assert.Equal(OperatorConsoleScreen.OperationsBoard, service.ActiveScreen);
        Assert.Equal(OperatorConsoleScreen.TransitControl, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.SurveyTelemetry, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.ReturnControl, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.ExpeditionRoster, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.SystemsBoard, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.MissionControl, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.OperationsBoard, service.CycleScreen(1));
        Assert.Equal(OperatorConsoleScreen.MissionControl, service.CycleScreen(-1));
    }

    private static OperationsBoardService CreateService(
        out TransitSimulationService transit,
        out ReturnSecurityService security,
        out ManualSimulationClock clock)
    {
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = CreateConnection(resources);
        transit = new TransitSimulationService(outgoing, new DestinationRegistry([Destination]), resources);
        security = new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter());
        clock = new ManualSimulationClock();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryService survey = CreateSurvey(roster, transit, clock);
        roster.BindSurvey(survey);
        ShiftIncidentService incidents = CreateIncidents(transit, security, roster, survey, resources);
        return new OperationsBoardService(transit, security, roster, survey, incidents, clock, resources);
    }

    private static ShiftIncidentService CreateIncidents(
        TransitSimulationService transit,
        ReturnSecurityService security,
        ExpeditionRosterService roster,
        SurveyTelemetryService survey,
        FacilityResourcePool resources) =>
        new(LoadIncidentCatalog(), transit, security, roster, survey, resources);

    private static IncidentCatalog LoadIncidentCatalog()
    {
        string root = FindRepositoryRoot();
        return new IncidentCatalogLoader().Load(File.ReadAllText(Path.Combine(root, "content", "incidents.v1.json")));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "project.godot")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Unable to locate repository root from test output.");
    }

    private static SurveyTelemetryService CreateSurvey(
        ExpeditionRosterService roster,
        TransitSimulationService transit,
        ManualSimulationClock clock) =>
        new(
            new SurveyDrone(),
            new SurveyTelemetryCatalog(
            [
                new SurveyTelemetryProfile(
                    Destination.Id,
                    0,
                    SurveyRiskAssessment.Acceptable,
                    "Test profile",
                    [
                        new SurveyChannelReading(
                            SurveyChannelKind.Atmosphere,
                            "Atmosphere",
                            "Clear",
                            SurveyReadingQuality.Clear,
                            null),
                    ]),
            ]),
            roster,
            transit,
            clock);

    private static OutgoingConnection CreateConnection(FacilityResourcePool resources) =>
        new(new DestinationRegistry([Destination]), resources);
}
