using FacilityCommand.Core.Incidents;

namespace FacilityCommand.Core.Shift;

/// <summary>
/// Transparent scoring from recorded debrief facts only — no hidden judgments.
/// </summary>
public static class ShiftScoring
{
    public static ShiftScore Evaluate(IReadOnlyList<IncidentDebriefFact> facts)
    {
        int success = facts.Count(fact => fact.Code.EndsWith("_success", StringComparison.Ordinal));
        int failure = facts.Count(fact => fact.Code.EndsWith("_failure", StringComparison.Ordinal));
        ShiftOutcomeCategory category;
        string rule;
        if (failure == 0)
        {
            category = ShiftOutcomeCategory.Nominal;
            rule = "Nominal: zero failure facts recorded.";
        }
        else if (failure < success)
        {
            category = ShiftOutcomeCategory.Contested;
            rule = "Contested: at least one failure fact, and failure facts are fewer than success facts.";
        }
        else
        {
            category = ShiftOutcomeCategory.Compromised;
            rule = "Compromised: failure facts are greater than or equal to success facts.";
        }

        return new ShiftScore(success, failure, facts.Count, category, rule);
    }
}
