namespace WormholeWorlds.Core.Shift;

public sealed record CampaignConsequence(
    string Code,
    CampaignConsequenceKind Kind,
    string Summary,
    string? RelatedEntityId = null);
