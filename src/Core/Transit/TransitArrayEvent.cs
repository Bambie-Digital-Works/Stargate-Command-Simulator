namespace FacilityCommand.Core.Transit;

public sealed record TransitArrayEvent(
    long Sequence,
    SimulationInstant At,
    TransitArrayEventKind Kind,
    TransitArrayCommandKind Command,
    TransitArrayPhase From,
    TransitArrayPhase To,
    TransitLinkDirection? Direction,
    string ReasonCode);
