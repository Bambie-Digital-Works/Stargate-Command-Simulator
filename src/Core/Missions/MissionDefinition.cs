namespace WormholeWorlds.Core.Missions;

public sealed record MissionDefinition(
    string Id,
    string DisplayName,
    string Summary,
    string DestinationId,
    string Objective,
    IReadOnlyList<string> RequiredSpecialties,
    int RiskRating,
    long FieldDurationMilliseconds,
    string ContactFaction);
