namespace FacilityCommand.Core.Shift;

public sealed record CampaignState(
    int SchemaVersion,
    IReadOnlyList<CampaignConsequence> Consequences,
    ShiftOutcomeCategory? LastOutcomeCategory,
    string? LastCategoryRuleSummary,
    DateTimeOffset? WrittenAtUtc = null)
{
    public const int CurrentSchemaVersion = 2;

    public static CampaignState Empty(int schemaVersion = CurrentSchemaVersion) =>
        new(schemaVersion, [], null, null, null);
}
