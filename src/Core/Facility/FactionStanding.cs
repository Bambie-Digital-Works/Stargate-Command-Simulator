namespace WormholeWorlds.Core.Facility;

public sealed record FactionStanding(
    string FactionId,
    string DisplayName,
    int Trust,
    string LatestContact);
