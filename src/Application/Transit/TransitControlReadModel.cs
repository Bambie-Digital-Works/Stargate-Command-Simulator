using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Transit;

public sealed record TransitControlReadModel(
    TransitArrayPhase Phase,
    string? DestinationId,
    IReadOnlyList<string> Vector,
    int LockedElements,
    int ReservedPower,
    int ReservedCooling,
    bool CanBeginSequence,
    bool CanLockVector,
    bool CanStabilize,
    bool CanAbort);
