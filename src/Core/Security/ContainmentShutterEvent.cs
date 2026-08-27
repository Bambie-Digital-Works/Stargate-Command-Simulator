using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Security;

public sealed record ContainmentShutterEvent(
    long Sequence,
    SimulationInstant At,
    ContainmentShutterCommandKind Command,
    ContainmentShutterState PreviousState,
    ContainmentShutterState State,
    string ReasonCode);
