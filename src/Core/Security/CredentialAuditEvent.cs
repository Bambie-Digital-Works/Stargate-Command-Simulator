using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Security;

public sealed record CredentialAuditEvent(
    long Sequence,
    SimulationInstant At,
    string? CredentialId,
    ReturnCredentialStatus Status,
    CredentialAuditOutcome Outcome,
    string ReasonCode);
