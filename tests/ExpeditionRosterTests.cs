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

public sealed class ExpeditionRosterTests
{
    private static readonly DestinationRecord Destination = new(
        "test_site",
        "Test Site",
        ["alpha", "bravo", "charlie", "delta"],
        40,
        25);

    private static readonly string[] ValidTeam =
    [
        "staff_harper",
        "staff_okoye",
        "staff_vesper",
        "staff_reed",
    ];

    [Fact]
    public void ValidTeamCanBeAssembledEquippedDispatchedAndTracked()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, OperationsBoardService board, SurveyTelemetryService survey) =
            CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Assembled, roster.Snapshot.State);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Equipped, roster.Snapshot.State);

        OpenStableLink(transit, clock);
        Assert.True(survey.Deploy().IsAccepted);
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Acceptable).IsAccepted);
        Assert.True(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Dispatched, roster.Snapshot.State);
        Assert.Contains("Dispatched", board.GetReadModel().ExpeditionUnitSummary, StringComparison.Ordinal);
        Assert.True(roster.Recall().IsAccepted);
        Assert.Equal(ExpeditionDispatchState.Standby, roster.Snapshot.State);
    }

    [Fact]
    public void MissingSpecialtyExplainsCorrectiveAction()
    {
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(new ManualSimulationClock());

        ExpeditionOperationResult result = roster.Assemble(["staff_harper", "staff_vesper", "staff_reed"]);

        Assert.False(result.IsAccepted);
        Assert.Equal("missing_specialty_medic", result.Rejection?.ReasonCode);
        Assert.Contains("Medic", result.Rejection!.CorrectiveAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DispatchRequiresStableTransitLink()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, _, _, _) = CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);

        ExpeditionOperationResult rejected = roster.Dispatch(transit.GetTransitArraySnapshot());

        Assert.False(rejected.IsAccepted);
        Assert.Equal("link_not_stable", rejected.Rejection?.ReasonCode);
        Assert.False(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
    }

    [Fact]
    public void DispatchRequiresSurveyRiskDecision()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, _, _) = CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        OpenStableLink(transit, clock);

        ExpeditionOperationResult rejected = roster.Dispatch(transit.GetTransitArraySnapshot());

        Assert.Equal("risk_decision_required", rejected.Rejection?.ReasonCode);
        Assert.False(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
    }

    [Fact]
    public void SnapshotRoundTripPreservesDispatchState()
    {
        (ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, _, SurveyTelemetryService survey) =
            CreateServices();
        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        OpenStableLink(transit, clock);
        Assert.True(survey.Deploy().IsAccepted);
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Elevated).IsAccepted);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);

        string json = roster.ExportSnapshotJson();
        ExpeditionRosterService restored = ExpeditionRosterService.CreateDefault(new ManualSimulationClock());
        restored.ImportSnapshotJson(json);

        Assert.Equal(ExpeditionDispatchState.Dispatched, restored.Snapshot.State);
        Assert.Equal(ValidTeam, restored.Snapshot.AssignedMemberIds);
        Assert.Equal(4, restored.Snapshot.EquippedItemIds.Count);
    }

    private static (
        ExpeditionRosterService Roster,
        TransitSimulationService Transit,
        ManualSimulationClock Clock,
        OperationsBoardService Board,
        SurveyTelemetryService Survey) CreateServices()
    {
        DestinationRegistry registry = new([Destination]);
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryService survey = new(
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
        roster.BindSurvey(survey);
        ShiftIncidentService incidents = new(
            new IncidentCatalogLoader().Load(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "content", "incidents.v1.json"))),
            transit,
            new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter()),
            roster,
            survey,
            resources);
        OperationsBoardService board = new(
            transit,
            new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter()),
            roster,
            survey,
            incidents,
            clock,
            resources);
        return (roster, transit, clock, board, survey);
    }

    private static void OpenStableLink(TransitSimulationService transit, ManualSimulationClock clock)
    {
        Assert.True(transit.PrepareSelected(Destination.Id, clock.Advance(1000)).IsAccepted);
        Assert.True(transit.BeginSequence(clock.Advance(1000)).IsAccepted);
        while (transit.GetTransitControl().CanLockVector)
        {
            Assert.True(transit.LockNextExpected().IsAccepted);
        }

        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
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
