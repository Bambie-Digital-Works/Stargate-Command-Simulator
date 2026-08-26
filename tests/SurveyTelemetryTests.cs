using FacilityCommand.Application.Operations;
using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Survey;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Personnel;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Survey;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Content;
using FacilityCommand.Infrastructure.Simulation;

namespace FacilityCommand.Tests;

public sealed class SurveyTelemetryTests
{
    private static readonly DestinationRecord Destination = new(
        "survey_site_aurora",
        "Aurora Survey Site",
        ["north_arc", "ember", "tidal", "verdant"],
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
    public void DeployIsGatedByLinkAndSurveyKit()
    {
        (SurveyTelemetryService survey, ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, _) =
            CreateServices();

        SurveyOperationResult withoutLink = survey.Deploy();
        Assert.Equal("destination_profile_missing", withoutLink.Rejection?.ReasonCode);

        OpenStableLink(transit, clock);
        SurveyOperationResult withoutKit = survey.Deploy();
        Assert.Equal("survey_kit_unavailable", withoutKit.Rejection?.ReasonCode);

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.True(survey.Deploy().IsAccepted);
        Assert.Equal(SurveyDeployState.Deployed, survey.Snapshot.State);
    }

    [Fact]
    public void DelayedTelemetryResolvesAndExposesContradiction()
    {
        (SurveyTelemetryService survey, ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, OperationsBoardService board) =
            CreateServices();

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        OpenStableLink(transit, clock);
        Assert.True(survey.Deploy().IsAccepted);

        SurveyOperationResult early = survey.ResolvePendingTelemetry();
        Assert.Equal("telemetry_delayed", early.Rejection?.ReasonCode);

        clock.Advance(1000);
        Assert.True(survey.ResolvePendingTelemetry().IsAccepted);
        SurveyTelemetryReadModel model = survey.GetReadModel();
        Assert.Equal(SurveyDeployState.TelemetryReady, survey.Snapshot.State);
        Assert.True(model.HasContradictoryReadings);
        Assert.Contains(model.Channels, channel => channel.QualityLabel == "Contradictory");
        Assert.Contains(board.GetReadModel().ActiveAlarms, alarm => alarm.Code == "survey_contradiction");
    }

    [Fact]
    public void RiskDecisionUnlocksDispatchAndImmediateProfileSkipsDelay()
    {
        DestinationRecord kestrel = new(
            "relay_station_kestrel",
            "Kestrel Relay Station",
            ["harbour", "silver", "zenith", "keystone"],
            55,
            35);
        (SurveyTelemetryService survey, ExpeditionRosterService roster, TransitSimulationService transit, ManualSimulationClock clock, OperationsBoardService board) =
            CreateServices(kestrel);

        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        OpenStableLink(transit, clock, kestrel.Id);

        Assert.True(survey.Deploy().IsAccepted);
        Assert.Equal(SurveyDeployState.TelemetryReady, survey.Snapshot.State);
        Assert.False(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);

        ExpeditionOperationResult blocked = roster.Dispatch(transit.GetTransitArraySnapshot());
        Assert.Equal("risk_decision_required", blocked.Rejection?.ReasonCode);

        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Acceptable).IsAccepted);
        Assert.True(roster.GetReadModel(transit.GetTransitArraySnapshot()).CanDispatch);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);
        Assert.Contains("decision Acceptable", board.GetReadModel().SurveyTelemetrySummary, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentCatalogLoadsStrictly()
    {
        string root = FindRepositoryRoot();
        string json = File.ReadAllText(Path.Combine(root, "content", "survey_profiles.v1.json"));

        SurveyTelemetryCatalog catalog = new SurveyTelemetryCatalogLoader().Load(json);

        Assert.True(catalog.TryGet("survey_site_aurora", out SurveyTelemetryProfile? aurora));
        Assert.NotNull(aurora);
        Assert.Contains(aurora!.Readings, reading => reading.Quality == SurveyReadingQuality.Contradictory);
        Assert.Throws<InvalidDataException>(() =>
            new SurveyTelemetryCatalogLoader().Load(json.Replace("\"schemaVersion\"", "\"unknown\"", StringComparison.Ordinal)));
    }

    private static (
        SurveyTelemetryService Survey,
        ExpeditionRosterService Roster,
        TransitSimulationService Transit,
        ManualSimulationClock Clock,
        OperationsBoardService Board) CreateServices(DestinationRecord? destination = null)
    {
        DestinationRecord active = destination ?? Destination;
        DestinationRegistry registry = new([active]);
        FacilityResourcePool resources = new(100, 100);
        OutgoingConnection outgoing = new(registry, resources);
        TransitSimulationService transit = new(outgoing, registry, resources);
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryCatalog catalog = LoadProductionCatalog();
        SurveyTelemetryService survey = new(new SurveyDrone(), catalog, roster, transit, clock);
        roster.BindSurvey(survey);
        OperationsBoardService board = new(
            transit,
            new ReturnSecurityService(new ReturnCredentialVerifier(), new ContainmentShutter()),
            roster,
            survey,
            clock,
            resources);
        return (survey, roster, transit, clock, board);
    }

    private static SurveyTelemetryCatalog LoadProductionCatalog()
    {
        string root = FindRepositoryRoot();
        string json = File.ReadAllText(Path.Combine(root, "content", "survey_profiles.v1.json"));
        return new SurveyTelemetryCatalogLoader().Load(json);
    }

    private static void OpenStableLink(
        TransitSimulationService transit,
        ManualSimulationClock clock,
        string? destinationId = null)
    {
        Assert.True(transit.PrepareSelected(destinationId ?? Destination.Id, clock.Advance(1000)).IsAccepted);
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
