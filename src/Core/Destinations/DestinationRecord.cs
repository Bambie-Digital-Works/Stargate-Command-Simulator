namespace FacilityCommand.Core.Destinations;

public sealed record DestinationRecord(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Vector,
    int RequiredPowerUnits,
    int RequiredCoolingUnits);
