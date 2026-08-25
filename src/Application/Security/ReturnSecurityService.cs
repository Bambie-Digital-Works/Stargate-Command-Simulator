using FacilityCommand.Core.Security;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Security;

public sealed class ReturnSecurityService
{
    private readonly ReturnCredentialVerifier _verifier;
    private readonly ContainmentShutter _shutter;
    private CredentialAssessment? _latestAssessment;

    public ReturnSecurityService(ReturnCredentialVerifier verifier, ContainmentShutter shutter)
    {
        _verifier = verifier;
        _shutter = shutter;
    }

    public CredentialVerificationResult VerifyCredential(
        ReturnCredential? credential,
        string challengeResponse,
        SimulationInstant at)
    {
        CredentialVerificationResult result = _verifier.Verify(credential, challengeResponse, at);
        _latestAssessment = result.Assessment;
        return result;
    }

    public ContainmentShutterResult ExecuteShutter(
        ContainmentShutterCommandKind kind,
        SimulationInstant at,
        TransitArraySnapshot transit,
        string? reasonCode = null) =>
        _shutter.Execute(new ContainmentShutterCommand(kind, at, _latestAssessment, reasonCode), transit);

    public ReturnSecurityReadModel GetReadModel(TransitArraySnapshot transit)
    {
        bool canOpen = _shutter.Snapshot.State == ContainmentShutterState.Closed
            && transit.Phase == TransitArrayPhase.LinkOpen
            && _latestAssessment?.IsAuthorized == true;
        return new ReturnSecurityReadModel(
            _shutter.Snapshot.State,
            _latestAssessment?.Status,
            _latestAssessment?.Outcome,
            _latestAssessment?.ReasonCode ?? "credential_not_checked",
            _latestAssessment?.CorrectiveAction ?? "Verify a Return Credential before changing containment.",
            canOpen,
            _latestAssessment?.Outcome == CredentialAuditOutcome.SecurityAlert);
    }
}
