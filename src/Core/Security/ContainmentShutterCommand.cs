using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Security;

public sealed record ContainmentShutterCommand(
    ContainmentShutterCommandKind Kind,
    SimulationInstant At,
    CredentialAssessment? Authorization = null,
    string? ReasonCode = null);
