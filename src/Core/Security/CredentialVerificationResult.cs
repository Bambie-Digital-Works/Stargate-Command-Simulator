namespace WormholeWorlds.Core.Security;

public sealed record CredentialVerificationResult(
    CredentialAssessment Assessment,
    CredentialAuditEvent AuditEvent);
