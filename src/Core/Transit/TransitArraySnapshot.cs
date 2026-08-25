namespace FacilityCommand.Core.Transit;

public sealed record TransitArraySnapshot(
    TransitArrayPhase Phase,
    TransitLinkDirection? Direction,
    SimulationInstant EnteredAt,
    long LastEventSequence,
    string LastReasonCode);
