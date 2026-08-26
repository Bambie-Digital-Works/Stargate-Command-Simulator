using FacilityCommand.Core.Security;

namespace FacilityCommand.Application.Security;

public sealed record ReturnCredentialScenario(
    string Id,
    string DisplayName,
    string WarningHint,
    ReturnCredentialStatus? ReportedStatus,
    string ChallengeProof,
    string ChallengeResponse,
    long ExpiresAtMilliseconds,
    string CredentialId);
