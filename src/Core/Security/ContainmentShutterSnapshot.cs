using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Security;

public sealed record ContainmentShutterSnapshot(
    ContainmentShutterState State,
    SimulationInstant ChangedAt,
    long LastEventSequence,
    string LastReasonCode)
{
    public bool IsSecured => State is ContainmentShutterState.Closed or ContainmentShutterState.Faulted;
}
