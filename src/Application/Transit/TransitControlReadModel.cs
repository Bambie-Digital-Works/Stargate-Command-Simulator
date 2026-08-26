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
    bool CanAbort,
    string PhaseLabel,
    string? NextLockElement,
    int FreePower,
    int FreeCooling,
    int PowerCapacity,
    int CoolingCapacity,
    bool CanPrepare,
    bool CanDetectIncoming,
    bool CanConfirmStable,
    bool CanCompleteRecovery,
    bool CanCompleteClosure,
    bool CanCompleteCooldown,
    string ProgressSummary);
