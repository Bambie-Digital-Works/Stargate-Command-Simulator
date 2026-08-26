namespace FacilityCommand.Core.Security;

public sealed record CredentialVerificationResult(
    CredentialAssessment Assessment,
    CredentialAuditEvent AuditEvent);
