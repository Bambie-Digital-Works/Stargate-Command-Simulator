using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Security;

public sealed record ReturnCredential(
    string CredentialId,
    string ExpeditionUnitId,
    string ChallengeProof,
    SimulationInstant ExpiresAt,
    ReturnCredentialStatus ReportedStatus = ReturnCredentialStatus.Verified);
