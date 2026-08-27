namespace WormholeWorlds.Core.Transit;

public enum TransitArrayEventKind
{
    PhaseChanged,
    AbortInitiated,
    TimeoutInitiated,
    FaultEntered,
    FaultResetInitiated,
}
