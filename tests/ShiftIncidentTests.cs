using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Survey;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Destinations;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Survey;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Content;
using WormholeWorlds.Infrastructure.Simulation;

namespace WormholeWorlds.Tests;

public sealed class ShiftIncidentTests
{
    private static readonly string[] ValidTeam =
    [
        "staff_harper",
        "staff_okoye",
        "staff_vesper",
        "staff_reed",
    ];

    [Fact]
    public void CatalogLoadsFiveOrderedIncidents()
    {
        IncidentCatalog catalog = LoadCatalog();
        Assert.Equal(5, catalog.Ordered.Count);
        Assert.Equal("routine_reconnaissance", catalog.Ordered[0].Id);
        Assert.Equal("medical_cooling_fault", catalog.Ordered[^1].Id);
    }

    [Fact]
    public void FullSuccessSequenceIsDeterministicAndCollectsDebriefFacts()
    {
        (ShiftIncidentService incidents, ExpeditionRosterService roster, TransitSimulationService transit,
            ReturnSecurityService security, SurveyTelemetryService survey, ManualSimulationClock clock, OperationsBoardService board) =
            CreateServices();

        Assert.Equal("routine_reconnaissance", incidents.GetReadModel().ActiveIncidentId);

        OpenOutgoing(transit, clock, "relay_station_kestrel");
        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.True(survey.Deploy().IsAccepted);
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Acceptable).IsAccepted);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);
        incidents.Evaluate();
        Assert.Equal("credential_damage", incidents.GetReadModel().ActiveIncidentId);

        security.VerifyScenario("damaged", clock.Advance(1000));
        incidents.Evaluate();
        Assert.Equal("spoofed_incoming", incidents.GetReadModel().ActiveIncidentId);

        Assert.True(transit.Abort(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteClosure(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteCooldown(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.DetectIncoming(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
        security.VerifyScenario("spoofed", clock.Advance(1000));
        incidents.Evaluate();
        Assert.Equal("survey_contradiction", incidents.GetReadModel().ActiveIncidentId);

        Assert.True(transit.Abort(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteClosure(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteCooldown(clock.Advance(1000)).IsAccepted);
        OpenOutgoing(transit, clock, "survey_site_aurora");
        Assert.True(roster.Recall().IsAccepted);
        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.True(survey.Deploy().IsAccepted);
        clock.Advance(1000);
        Assert.True(survey.ResolvePendingTelemetry().IsAccepted);
        Assert.True(survey.GetReadModel().HasContradictoryReadings);
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Elevated).IsAccepted);
        incidents.Evaluate();
        Assert.Equal("medical_cooling_fault", incidents.GetReadModel().ActiveIncidentId);
        Assert.True(incidents.Snapshot.DebriefFacts.Count >= 4);
        Assert.True(board.GetReadModel().FreeCooling < 100);
        Assert.Contains(roster.Pool, member => member.IsInjured);

        Assert.True(incidents.ResolveCoolingFault().IsAccepted);
        IncidentProgressReadModel complete = incidents.GetReadModel();
        Assert.True(complete.SequenceComplete);
        Assert.Equal(5, complete.CompletedCount);
        Assert.Equal(0, complete.FailedCount);
        Assert.Equal(5, complete.DebriefFactSummaries.Count);
        Assert.Contains("complete", board.GetReadModel().IncidentSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FailureBranchAdvancesAndRecordsFailureFact()
    {
        (ShiftIncidentService incidents, _, _, _, _, _, _) = CreateServices();

        Assert.True(incidents.Observe("routine_aborted").IsAccepted);
        Assert.Equal("credential_damage", incidents.GetReadModel().ActiveIncidentId);
        Assert.Equal(1, incidents.Snapshot.FailedCount);
        Assert.Contains(
            incidents.CollectDebriefFacts(),
            fact => fact.Code == "routine_reconnaissance_failure");
    }

    [Fact]
    public void IdenticalManualEventSequencesMatchAcrossRuns()
    {
        string[] events =
        [
            "expedition_dispatched",
            "credential_damaged_verified",
            "credential_spoof_verified",
            "contradiction_risk_recorded",
            "combined_crisis_contained",
        ];

        string[] first = RunManual(events);
        string[] second = RunManual(events);
        Assert.Equal(first, second);
    }

    private static string[] RunManual(IReadOnlyList<string> events)
    {
        IncidentScript script = new(LoadCatalog());
        foreach (string eventCode in events)
        {
            Assert.True(script.Observe(eventCode).IsAccepted);
        }

        return script.Snapshot.DebriefFacts.Select(fact => $"{fact.Code}:{fact.Summary}").ToArray();
    }

    private static (
        ShiftIncidentService Incidents,
        ExpeditionRosterService Roster,
        TransitSimulationService Transit,
        ReturnSecurityService Security,
        SurveyTelemetryService Survey,
        ManualSimulationClock Clock,
        OperationsBoardService Board) CreateServices()
    {
        DestinationRecord kestrel = new(
            "relay_station_kestrel",
            "Kestrel Relay Station",
            ["harbour", "silver", "zenith", "keystone"],
            55,
            35);
        DestinationRecord aurora = new(
            "survey_site_aurora",
            "Aurora Survey Site",
            ["north_arc", "ember", "tidal", "verdant"],
            40,
            25);
        DestinationRegistry registry = new([kestrel, aurora]);
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryCatalog surveyCatalog = new SurveyTelemetryCatalogLoader().Load(
            File.ReadAllText(Path.Combine(FindRepositoryRoot(), "content", "survey_profiles.v1.json")));
        SurveyTelemetryService survey = new(new SurveyDrone(), surveyCatalog, roster, transit, clock);
        roster.BindSurvey(survey);
        ShiftIncidentService incidents = new(LoadCatalog(), transit, security, roster, survey, resources);
        OperationsBoardService board = new(transit, security, roster, survey, incidents, clock, resources);
        return (incidents, roster, transit, security, survey, clock, board);
    }

    private static void OpenOutgoing(TransitSimulationService transit, ManualSimulationClock clock, string destinationId)
    {
        Assert.True(transit.PrepareSelected(destinationId, clock.Advance(1000)).IsAccepted);
        Assert.True(transit.BeginSequence(clock.Advance(1000)).IsAccepted);
        while (transit.GetTransitControl().CanLockVector)
        {
            Assert.True(transit.LockNextExpected().IsAccepted);
        }

        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
    }

    private static IncidentCatalog LoadCatalog()
    {
        string json = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "content", "incidents.v1.json"));
        return new IncidentCatalogLoader().Load(json);
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
}
