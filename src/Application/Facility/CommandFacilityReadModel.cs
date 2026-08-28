using WormholeWorlds.Core.Facility;

namespace WormholeWorlds.Application.Facility;

public sealed record CommandFacilityReadModel(
    IReadOnlyList<FacilitySystemStatus> Systems,
    IReadOnlyList<FacilityZoneStatus> Zones,
    IReadOnlyList<DestinationIntel> Intelligence,
    IReadOnlyList<FactionStanding> Factions,
    IReadOnlyList<string> Alerts,
    string Summary);
