using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Security;

public sealed record ContainmentShutterCommand(
    ContainmentShutterCommandKind Kind,
    SimulationInstant At,
    CredentialAssessment? Authorization = null,
    string? ReasonCode = null);
