using FacilityCommand.Core.Security;

namespace FacilityCommand.Application.Security;

public sealed record ReturnSecurityReadModel(
    ContainmentShutterState ShutterState,
    ReturnCredentialStatus? CredentialStatus,
    CredentialAuditOutcome? AuthorizationOutcome,
    string StatusCode,
    string CorrectiveAction,
    bool CanOpenShutter,
    bool HasSecurityAlert);
