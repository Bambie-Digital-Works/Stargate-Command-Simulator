namespace WormholeWorlds.Core.Facility;

public sealed record FacilityZoneStatus(
    string Id,
    string DisplayName,
    bool LockedDown,
    string AccessLevel);
