using WormholeWorlds.Core.Security;

namespace WormholeWorlds.Application.Security;

public sealed record ReturnSecurityReadModel(
    ContainmentShutterState ShutterState,
    ReturnCredentialStatus? CredentialStatus,
    CredentialAuditOutcome? AuthorizationOutcome,
    string StatusCode,
    string CorrectiveAction,
    bool CanOpenShutter,
    bool HasSecurityAlert,
    string WarningCategory,
    string WarningSummary,
    string ShutterStateLabel,
    bool LinkIsStable,
    bool CanConfirmOpened,
    bool CanCloseShutter,
    bool CanConfirmClosed,
    bool CanReportFault,
    bool CanResetFault);
