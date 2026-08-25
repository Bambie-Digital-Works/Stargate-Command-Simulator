namespace FacilityCommand.Core.Transit;

public sealed record OutgoingConnectionSnapshot(
    TransitArraySnapshot TransitArray,
    string? DestinationId,
    IReadOnlyList<string> Vector,
    int LockedElements,
    int ReservedPower,
    int ReservedCooling);
