using WormholeWorlds.Core.Incidents;

namespace WormholeWorlds.Core.Shift;

public sealed record ShiftReviewSummary(
    IReadOnlyList<IncidentDebriefFact> ChronologicalFacts,
    ShiftScore Score,
    IReadOnlyList<CampaignConsequence> Consequences);
