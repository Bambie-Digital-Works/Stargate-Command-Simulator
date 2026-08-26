using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Security;

public sealed record ReturnCredential(
    string CredentialId,
    string ExpeditionUnitId,
    string ChallengeProof,
    SimulationInstant ExpiresAt,
    ReturnCredentialStatus ReportedStatus = ReturnCredentialStatus.Verified);
