using WormholeWorlds.Core.Incidents;

namespace WormholeWorlds.Core.Shift;

/// <summary>
/// Derives persisted campaign consequences from debrief facts and live facility state.
/// </summary>
public static class CampaignConsequenceDeriver
{
    public static IReadOnlyList<CampaignConsequence> Derive(
        IReadOnlyList<IncidentDebriefFact> facts,
        IReadOnlyList<(string MemberId, string DisplayName)> injuredMembers,
        bool coolingFaultActive)
    {
        List<CampaignConsequence> consequences = [];

        foreach ((string memberId, string displayName) in injuredMembers)
        {
            consequences.Add(new CampaignConsequence(
                $"injured_{memberId}",
                CampaignConsequenceKind.InjuredStaff,
                $"{displayName} remains injured and unavailable until medical clearance.",
                memberId));
        }

        if (coolingFaultActive)
        {
            consequences.Add(new CampaignConsequence(
                "cooling_fault_residual",
                CampaignConsequenceKind.CoolingResidual,
                "Cooling capacity remains degraded until Systems clears the residual fault."));
        }

        foreach (IncidentDebriefFact fact in facts)
        {
            if (fact.Code.EndsWith("_success", StringComparison.Ordinal))
            {
                consequences.Add(new CampaignConsequence(
                    $"discovery_{fact.Code}",
                    CampaignConsequenceKind.Discovery,
                    fact.Summary));
            }
        }

        ShiftScore score = ShiftScoring.Evaluate(facts);
        consequences.Add(new CampaignConsequence(
            $"outcome_{score.Category.ToString().ToLowerInvariant()}",
            CampaignConsequenceKind.CommandNote,
            $"Prior shift closed as {score.Category}: {score.CategoryRuleSummary}"));

        return consequences;
    }
}
