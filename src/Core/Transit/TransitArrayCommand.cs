namespace WormholeWorlds.Core.Transit;

public sealed record TransitArrayCommand(
    TransitArrayCommandKind Kind,
    SimulationInstant At,
    string? ReasonCode = null);
