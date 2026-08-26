using FacilityCommand.Core.Incidents;

namespace FacilityCommand.Core.Shift;

public sealed record ShiftReviewSummary(
    IReadOnlyList<IncidentDebriefFact> ChronologicalFacts,
    ShiftScore Score,
    IReadOnlyList<CampaignConsequence> Consequences);
