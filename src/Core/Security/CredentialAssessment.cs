namespace WormholeWorlds.Core.Security;

public sealed record CredentialAssessment(
    ReturnCredentialStatus Status,
    CredentialAuditOutcome Outcome,
    string ReasonCode,
    string CorrectiveAction)
{
    public bool IsAuthorized => Outcome == CredentialAuditOutcome.Authorized;
}
