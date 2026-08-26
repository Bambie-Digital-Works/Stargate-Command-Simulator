using FacilityCommand.Application.Incidents;
using FacilityCommand.Application.Operations;
using FacilityCommand.Application.Personnel;
using FacilityCommand.Application.Security;
using FacilityCommand.Application.Shift;
using FacilityCommand.Application.Survey;
using FacilityCommand.Application.Transit;
using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Incidents;
using FacilityCommand.Core.Personnel;
using FacilityCommand.Core.Security;
using FacilityCommand.Core.Shift;
using FacilityCommand.Core.Survey;
using FacilityCommand.Core.Transit;
using FacilityCommand.Infrastructure.Content;
using FacilityCommand.Infrastructure.Persistence;
using FacilityCommand.Infrastructure.Simulation;

namespace FacilityCommand.Tests;

public sealed class ShiftLifecycleTests
{
    private static readonly string[] ValidTeam =
    [
        "staff_harper",
        "staff_okoye",
        "staff_vesper",
        "staff_reed",
    ];

    [Fact]
    public void BriefLoadsAndStartShiftOpensOperationsBoard()
    {
        using TempCampaignPath temp = new();
        (ShiftLifecycleService lifecycle, OperationsBoardService board, _, _, _, _, _, _) = CreateLifecycle(temp.Path);

        ShiftBriefReadModel brief = lifecycle.GetBriefReadModel();
        Assert.Equal(ShiftPhase.Briefing, brief.Phase);
        Assert.True(brief.CanStartShift);
        Assert.Contains(brief.Objectives, line => line.Contains("Transit", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(brief.CarryoverLines, line => line.Contains("No prior-shift", StringComparison.OrdinalIgnoreCase));

        Assert.True(lifecycle.StartShift().IsAccepted);
        Assert.Equal(ShiftPhase.InProgress, lifecycle.Phase);
        Assert.Equal(OperatorConsoleScreen.OperationsBoard, board.ActiveScreen);
        Assert.False(lifecycle.GetBriefReadModel().CanStartShift);
    }

    [Fact]
    public void ScoringIsTransparentFromSuccessAndFailureFacts()
    {
        Assert.Equal(
            ShiftOutcomeCategory.Nominal,
            ShiftScoring.Evaluate(
            [
                new IncidentDebriefFact("a_success", "ok"),
                new IncidentDebriefFact("b_success", "ok"),
            ]).Category);

        Assert.Equal(
            ShiftOutcomeCategory.Contested,
            ShiftScoring.Evaluate(
            [
                new IncidentDebriefFact("a_success", "ok"),
                new IncidentDebriefFact("b_success", "ok"),
                new IncidentDebriefFact("c_failure", "miss"),
            ]).Category);

        Assert.Equal(
            ShiftOutcomeCategory.Compromised,
            ShiftScoring.Evaluate(
            [
                new IncidentDebriefFact("a_success", "ok"),
                new IncidentDebriefFact("b_failure", "miss"),
                new IncidentDebriefFact("c_failure", "miss"),
            ]).Category);
    }

    [Fact]
    public void FullSuccessShiftEndsInNominalReviewAndPersistsCarryover()
    {
        using TempCampaignPath temp = new();
        (ShiftLifecycleService lifecycle, OperationsBoardService board, ShiftIncidentService incidents,
            ExpeditionRosterService roster, TransitSimulationService transit, ReturnSecurityService security,
            SurveyTelemetryService survey, ManualSimulationClock clock) = CreateLifecycle(temp.Path);

        Assert.True(lifecycle.StartShift().IsAccepted);
        DriveFullSuccess(incidents, roster, transit, security, survey, clock);
        Assert.True(board.GetReadModel().CanEndShift);
        Assert.True(lifecycle.EndShift().IsAccepted);

        ShiftReviewReadModel review = lifecycle.GetReviewReadModel();
        Assert.Equal(ShiftPhase.Review, review.Phase);
        Assert.Equal(nameof(ShiftOutcomeCategory.Nominal), review.OutcomeCategoryLabel);
        Assert.Equal(5, review.SuccessFactCount);
        Assert.Equal(0, review.FailureFactCount);
        Assert.True(review.ChronologicalFactLines.Count >= 5);
        Assert.Contains(review.ConsequenceLines, line => line.Contains("InjuredStaff", StringComparison.Ordinal));
        Assert.Equal(OperatorConsoleScreen.ShiftReview, board.ActiveScreen);

        CampaignState reloaded = new CampaignStateStore(temp.Path).LoadOrEmpty();
        Assert.Equal(ShiftOutcomeCategory.Nominal, reloaded.LastOutcomeCategory);
        Assert.Contains(reloaded.Consequences, item => item.Kind == CampaignConsequenceKind.InjuredStaff);

        Assert.True(lifecycle.BeginNextShift().IsAccepted);
        Assert.Equal(ShiftPhase.Briefing, lifecycle.Phase);
        Assert.Contains(
            lifecycle.GetBriefReadModel().CarryoverLines,
            line => line.Contains("injured", StringComparison.OrdinalIgnoreCase)
                || line.Contains("Prior outcome", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(roster.Pool, member => member.IsInjured);
        Assert.Equal("routine_reconnaissance", incidents.GetReadModel().ActiveIncidentId);
    }

    [Fact]
    public void FailureHeavyShiftScoresCompromised()
    {
        using TempCampaignPath temp = new();
        (ShiftLifecycleService lifecycle, _, ShiftIncidentService incidents, _, _, _, _, _) = CreateLifecycle(temp.Path);

        Assert.True(lifecycle.StartShift().IsAccepted);
        for (int i = 0; i < 5; i++)
        {
            string? failure = incidents.GetReadModel().ActiveIncidentId switch
            {
                "routine_reconnaissance" => "routine_aborted",
                "credential_damage" => "credential_damage_bypass",
                "spoofed_incoming" => "spoofed_incoming_missed",
                "survey_contradiction" => "contradiction_ignored",
                "medical_cooling_fault" => "combined_crisis_unresolved",
                _ => null,
            };
            Assert.NotNull(failure);
            Assert.True(incidents.Observe(failure!).IsAccepted);
        }

        Assert.True(lifecycle.EndShift().IsAccepted);
        ShiftReviewReadModel review = lifecycle.GetReviewReadModel();
        Assert.Equal(nameof(ShiftOutcomeCategory.Compromised), review.OutcomeCategoryLabel);
        Assert.Equal(0, review.SuccessFactCount);
        Assert.Equal(5, review.FailureFactCount);
    }

    [Fact]
    public void CampaignRoundTripInfluencesNextBriefWithoutLifecycle()
    {
        using TempCampaignPath temp = new();
        CampaignStateStore store = new(temp.Path);
        store.Save(new CampaignState(
            1,
            [
                new CampaignConsequence(
                    "injured_staff_harper",
                    CampaignConsequenceKind.InjuredStaff,
                    "Harper remains injured.",
                    "staff_harper"),
            ],
            ShiftOutcomeCategory.Contested,
            "Contested: at least one failure fact, and failure facts are fewer than success facts."));

        (ShiftLifecycleService lifecycle, _, _, ExpeditionRosterService roster, _, _, _, _) = CreateLifecycle(temp.Path);
        Assert.Contains(roster.Pool, member => member.Id == "staff_harper" && member.IsInjured);
        Assert.Contains(
            lifecycle.GetBriefReadModel().CarryoverLines,
            line => line.Contains("Harper", StringComparison.OrdinalIgnoreCase));
    }

    private static void DriveFullSuccess(
        ShiftIncidentService incidents,
        ExpeditionRosterService roster,
        TransitSimulationService transit,
        ReturnSecurityService security,
        SurveyTelemetryService survey,
        ManualSimulationClock clock)
    {
        OpenOutgoing(transit, clock, "relay_station_kestrel");
        Assert.True(roster.Assemble(ValidTeam).IsAccepted);
        Assert.True(roster.EquipRequiredKit().IsAccepted);
        Assert.True(survey.Deploy().IsAccepted);
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Acceptable).IsAccepted);
        Assert.True(roster.Dispatch(transit.GetTransitArraySnapshot()).IsAccepted);
        incidents.Evaluate();

        security.VerifyScenario("damaged", clock.Advance(1000));
        incidents.Evaluate();

        Assert.True(transit.Abort(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteClosure(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.CompleteCooldown(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.DetectIncoming(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.BeginStabilization(clock.Advance(1000)).IsAccepted);
        Assert.True(transit.ConfirmStable(clock.Advance(1000)).IsAccepted);
        security.VerifyScenario("spoofed", clock.Advance(1000));
        incidents.Evaluate();

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
        Assert.True(survey.RecordRiskDecision(SurveyRiskAssessment.Elevated).IsAccepted);
        incidents.Evaluate();
        Assert.True(incidents.ResolveCoolingFault().IsAccepted);
        Assert.True(incidents.GetReadModel().SequenceComplete);
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

    private static (
        ShiftLifecycleService Lifecycle,
        OperationsBoardService Board,
        ShiftIncidentService Incidents,
        ExpeditionRosterService Roster,
        TransitSimulationService Transit,
        ReturnSecurityService Security,
        SurveyTelemetryService Survey,
        ManualSimulationClock Clock) CreateLifecycle(string campaignPath)
    {
        string root = FindRepositoryRoot();
        FacilityResourcePool resources = new(100, 100);
        DestinationRegistry destinations = new DestinationRegistryLoader().Load(
            File.ReadAllText(Path.Combine(root, "content", "destinations.v1.json")));
        OutgoingConnection outgoing = new(destinations, resources);
        TransitSimulationService transit = new(outgoing, destinations, resources);
        ReturnSecurityService security = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        ManualSimulationClock clock = new();
        ExpeditionRosterService roster = ExpeditionRosterService.CreateDefault(clock);
        SurveyTelemetryCatalog surveyCatalog = new SurveyTelemetryCatalogLoader().Load(
            File.ReadAllText(Path.Combine(root, "content", "survey_profiles.v1.json")));
        SurveyTelemetryService survey = new(new SurveyDrone(), surveyCatalog, roster, transit, clock);
        roster.BindSurvey(survey);
        IncidentCatalog incidentsCatalog = new IncidentCatalogLoader().Load(
            File.ReadAllText(Path.Combine(root, "content", "incidents.v1.json")));
        ShiftIncidentService incidents = new(incidentsCatalog, transit, security, roster, survey, resources);
        OperationsBoardService board = new(transit, security, roster, survey, incidents, clock, resources);
        ShiftBriefDefinition brief = new ShiftBriefLoader().Load(
            File.ReadAllText(Path.Combine(root, "content", "shift_brief.v1.json")));
        CampaignStateStore store = new(campaignPath);
        ShiftLifecycleService lifecycle = new(brief, incidents, roster, resources, store, board);
        board.BindLifecycle(lifecycle);
        return (lifecycle, board, incidents, roster, transit, security, survey, clock);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "project.godot")))
        {
            current = current.Parent;
        }

        return current?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from test output.");
    }

    private sealed class TempCampaignPath : IDisposable
    {
        public TempCampaignPath()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"fc-campaign-{Guid.NewGuid():N}.json");
        }

        public string Path { get; }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }

            if (File.Exists(Path + ".tmp"))
            {
                File.Delete(Path + ".tmp");
            }
        }
    }
}
