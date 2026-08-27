using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Application.Personnel;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Personnel;
using WormholeWorlds.Core.Shift;
using WormholeWorlds.Core.Transit;
using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Application.Shift;

public sealed class ShiftLifecycleService
{
    private readonly ShiftBriefDefinition _brief;
    private readonly ShiftIncidentService _incidents;
    private readonly ExpeditionRosterService _roster;
    private readonly FacilityResourcePool _resources;
    private readonly CampaignStateStore _store;
    private readonly OperationsBoardService _operations;
    private CampaignState _campaign;
    private ShiftPhase _phase = ShiftPhase.Briefing;
    private ShiftReviewSummary? _latestReview;
    private string? _persistenceGuidance;

    public ShiftLifecycleService(
        ShiftBriefDefinition brief,
        ShiftIncidentService incidents,
        ExpeditionRosterService roster,
        FacilityResourcePool resources,
        CampaignStateStore store,
        OperationsBoardService operations)
    {
        _brief = brief;
        _incidents = incidents;
        _roster = roster;
        _resources = resources;
        _store = store;
        _operations = operations;
        CampaignSaveLoadResult load = store.Load();
        _campaign = load.State;
        _persistenceGuidance = load.RecoveryGuidance;
        ApplyCarryoverEffects();
        _operations.SetActiveScreen(OperatorConsoleScreen.ShiftBrief);
    }

    public ShiftPhase Phase => _phase;

    public CampaignState Campaign => _campaign;

    public string? PersistenceGuidance => _persistenceGuidance;

    public ShiftBriefReadModel GetBriefReadModel()
    {
        List<string> carryover = BuildCarryoverLines();
        return new ShiftBriefReadModel(
            _brief.Title,
            _brief.Summary,
            _brief.Objectives,
            _brief.KnownRisks,
            _brief.ReadinessLines,
            _brief.Constraints,
            carryover,
            _phase,
            _phase == ShiftPhase.Briefing,
            _persistenceGuidance);
    }

    public ShiftReviewReadModel GetReviewReadModel()
    {
        if (_latestReview is null)
        {
            return new ShiftReviewReadModel(
                "No review",
                "End the shift after incidents complete to generate a Shift Review.",
                0,
                0,
                0,
                [],
                [],
                _phase,
                false);
        }

        ShiftScore score = _latestReview.Score;
        return new ShiftReviewReadModel(
            score.Category.ToString(),
            score.CategoryRuleSummary,
            score.SuccessFactCount,
            score.FailureFactCount,
            score.TotalFactCount,
            _latestReview.ChronologicalFacts.Select(fact => $"[{fact.Code}] {fact.Summary}").ToArray(),
            _latestReview.Consequences.Select(item => $"[{item.Kind}] {item.Summary}").ToArray(),
            _phase,
            _phase == ShiftPhase.Review);
    }

    public ShiftOperationResult StartShift()
    {
        if (_phase != ShiftPhase.Briefing)
        {
            return ShiftOperationResult.Rejected("shift_not_briefing", "Acknowledge the Shift Brief only while briefing is active.");
        }

        _phase = ShiftPhase.InProgress;
        _operations.SetActiveScreen(OperatorConsoleScreen.OperationsBoard);
        return ShiftOperationResult.Accepted();
    }

    public ShiftOperationResult EndShift()
    {
        if (_phase != ShiftPhase.InProgress)
        {
            return ShiftOperationResult.Rejected("shift_not_in_progress", "End shift is only available during an active shift.");
        }

        IncidentProgressReadModel progress = _incidents.GetReadModel();
        if (!progress.SequenceComplete)
        {
            return ShiftOperationResult.Rejected(
                "incidents_incomplete",
                "Resolve or fail all five vertical-slice incidents before ending the shift.");
        }

        IReadOnlyList<IncidentDebriefFact> facts = _incidents.CollectDebriefFacts();
        List<(string MemberId, string DisplayName)> injured = _roster.Pool
            .Where(member => member.IsInjured)
            .Select(member => (member.Id, member.DisplayName))
            .ToList();
        IReadOnlyList<CampaignConsequence> consequences = CampaignConsequenceDeriver.Derive(
            facts,
            injured,
            _resources.CoolingFaultHold > 0);
        ShiftScore score = ShiftScoring.Evaluate(facts);
        _latestReview = new ShiftReviewSummary(facts, score, consequences);
        _campaign = new CampaignState(
            _store.SupportedSchemaVersion,
            consequences,
            score.Category,
            score.CategoryRuleSummary,
            DateTimeOffset.UtcNow);
        _store.Save(_campaign);
        _persistenceGuidance = null;
        _phase = ShiftPhase.Review;
        _operations.SetActiveScreen(OperatorConsoleScreen.ShiftReview);
        return ShiftOperationResult.Accepted();
    }

    public ShiftOperationResult BeginNextShift()
    {
        if (_phase != ShiftPhase.Review)
        {
            return ShiftOperationResult.Rejected("shift_not_review", "Begin the next shift from Shift Review.");
        }

        _incidents.Reset();
        _latestReview = null;
        ApplyCarryoverEffects();
        _phase = ShiftPhase.Briefing;
        _operations.SetActiveScreen(OperatorConsoleScreen.ShiftBrief);
        return ShiftOperationResult.Accepted();
    }

    private void ApplyCarryoverEffects()
    {
        foreach (CampaignConsequence consequence in _campaign.Consequences)
        {
            if (consequence.Kind == CampaignConsequenceKind.InjuredStaff
                && !string.IsNullOrWhiteSpace(consequence.RelatedEntityId))
            {
                _roster.MarkInjured(consequence.RelatedEntityId);
            }

            if (consequence.Kind == CampaignConsequenceKind.CoolingResidual
                && _resources.CoolingFaultHold <= 0)
            {
                _resources.TryApplyCoolingFault(40);
            }
        }
    }

    private List<string> BuildCarryoverLines()
    {
        List<string> lines = [];
        if (!string.IsNullOrWhiteSpace(_persistenceGuidance))
        {
            lines.Add($"Save recovery: {_persistenceGuidance}");
        }

        if (_campaign.LastOutcomeCategory is { } category)
        {
            lines.Add($"Prior outcome: {category}. {_campaign.LastCategoryRuleSummary}");
        }

        foreach (CampaignConsequence consequence in _campaign.Consequences)
        {
            if (consequence.Kind is CampaignConsequenceKind.InjuredStaff
                or CampaignConsequenceKind.CoolingResidual
                or CampaignConsequenceKind.CommandNote)
            {
                lines.Add(consequence.Summary);
            }
        }

        if (lines.Count == 0)
        {
            lines.Add("No prior-shift consequences on file.");
        }

        return lines;
    }
}
