namespace FacilityCommand.Core.Shift;

public sealed record CampaignState(
    int SchemaVersion,
    IReadOnlyList<CampaignConsequence> Consequences,
    ShiftOutcomeCategory? LastOutcomeCategory,
    string? LastCategoryRuleSummary)
{
    public static CampaignState Empty(int schemaVersion = 1) =>
        new(schemaVersion, [], null, null);
}
