namespace FacilityCommand.Application.Transit;

public sealed record DestinationOption(
    string Id,
    string DisplayName,
    IReadOnlyList<string> Vector,
    int RequiredPowerUnits,
    int RequiredCoolingUnits);
