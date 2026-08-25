using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Security;

public sealed class ReturnCredentialVerifier
{
    private readonly HashSet<string> _acceptedCredentialIds = new(StringComparer.Ordinal);
    private long _auditSequence;

    public CredentialVerificationResult Verify(
        ReturnCredential? credential,
        string challengeResponse,
        SimulationInstant at)
    {
        ReturnCredentialStatus status = ResolveStatus(credential, challengeResponse, at);
        CredentialAssessment assessment = Assess(status);
        if (credential is not null && assessment.IsAuthorized)
        {
            _acceptedCredentialIds.Add(credential.CredentialId);
        }

        CredentialAuditEvent auditEvent = new(
            ++_auditSequence,
            at,
            credential?.CredentialId,
            status,
            assessment.Outcome,
            assessment.ReasonCode);
        return new CredentialVerificationResult(assessment, auditEvent);
    }

    private ReturnCredentialStatus ResolveStatus(
        ReturnCredential? credential,
        string challengeResponse,
        SimulationInstant at)
    {
        if (credential is null || credential.ReportedStatus == ReturnCredentialStatus.Missing)
        {
            return ReturnCredentialStatus.Missing;
        }

        if (credential.ReportedStatus != ReturnCredentialStatus.Verified)
        {
            return credential.ReportedStatus;
        }

        if (at.Milliseconds > credential.ExpiresAt.Milliseconds)
        {
            return ReturnCredentialStatus.Expired;
        }

        if (_acceptedCredentialIds.Contains(credential.CredentialId))
        {
            return ReturnCredentialStatus.Duplicate;
        }

        return string.Equals(challengeResponse, credential.ChallengeProof, StringComparison.Ordinal)
            ? ReturnCredentialStatus.Verified
            : ReturnCredentialStatus.SpoofSuspected;
    }

    private static CredentialAssessment Assess(ReturnCredentialStatus status) => status switch
    {
        ReturnCredentialStatus.Verified => new(status, CredentialAuditOutcome.Authorized, "credential_verified", "Authorization is valid for the current incoming link."),
        ReturnCredentialStatus.Duress => new(status, CredentialAuditOutcome.SecurityAlert, "credential_duress", "Keep the Containment Shutter closed and begin the duress response."),
        ReturnCredentialStatus.SpoofSuspected => new(status, CredentialAuditOutcome.SecurityAlert, "credential_spoof_suspected", "Keep the Containment Shutter closed and escalate the identity challenge."),
        ReturnCredentialStatus.Missing => new(status, CredentialAuditOutcome.Withheld, "credential_missing", "Request a valid Return Credential before opening the Containment Shutter."),
        ReturnCredentialStatus.Damaged => new(status, CredentialAuditOutcome.Withheld, "credential_damaged", "Use the damaged-credential challenge procedure before authorization."),
        ReturnCredentialStatus.Expired => new(status, CredentialAuditOutcome.Withheld, "credential_expired", "Request a current Return Credential before authorization."),
        ReturnCredentialStatus.Duplicate => new(status, CredentialAuditOutcome.Withheld, "credential_duplicate", "Reject the replayed credential and request a new challenge."),
        _ => throw new ArgumentOutOfRangeException(nameof(status)),
    };
}
