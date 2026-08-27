using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Security;

public sealed class ReturnSecurityService
{
    private static readonly IReadOnlyList<ReturnCredentialScenario> Scenarios =
    [
        new(
            "friendly",
            "Friendly verified return",
            "Authorized when the challenge matches and the Transit Link is stable.",
            ReturnCredentialStatus.Verified,
            "proof-alpha",
            "proof-alpha",
            60_000,
            "credential_friendly"),
        new(
            "missing",
            "Missing credential",
            "Rejected: no Return Credential presented.",
            null,
            "proof-alpha",
            "proof-alpha",
            60_000,
            "credential_missing"),
        new(
            "damaged",
            "Damaged credential",
            "Rejected: credential evidence is damaged.",
            ReturnCredentialStatus.Damaged,
            "proof-alpha",
            "proof-alpha",
            60_000,
            "credential_damaged"),
        new(
            "duress",
            "Duress signal",
            "Dangerous: duress response withholds authorization.",
            ReturnCredentialStatus.Duress,
            "proof-alpha",
            "proof-alpha",
            60_000,
            "credential_duress"),
        new(
            "spoofed",
            "Spoofed challenge",
            "Dangerous: challenge mismatch escalates identity challenge.",
            ReturnCredentialStatus.Verified,
            "proof-alpha",
            "wrong-proof",
            60_000,
            "credential_spoof"),
    ];

    private readonly ReturnCredentialVerifier _verifier;
    private readonly ContainmentShutter _shutter;
    private CredentialAssessment? _latestAssessment;

    public ReturnSecurityService(ReturnCredentialVerifier verifier, ContainmentShutter shutter)
    {
        _verifier = verifier;
        _shutter = shutter;
    }

    public IReadOnlyList<ReturnCredentialScenario> ListScenarios() => Scenarios;

    public CredentialVerificationResult VerifyCredential(
        ReturnCredential? credential,
        string challengeResponse,
        SimulationInstant at)
    {
        CredentialVerificationResult result = _verifier.Verify(credential, challengeResponse, at);
        _latestAssessment = result.Assessment;
        return result;
    }

    public CredentialVerificationResult VerifyScenario(string scenarioId, SimulationInstant at)
    {
        ReturnCredentialScenario scenario = Scenarios.FirstOrDefault(item => item.Id == scenarioId)
            ?? throw new ArgumentOutOfRangeException(nameof(scenarioId), scenarioId, "Unknown Return Credential scenario.");

        ReturnCredential? credential = scenario.ReportedStatus is null
            ? null
            : new ReturnCredential(
                scenario.CredentialId,
                "expedition_unit_return",
                scenario.ChallengeProof,
                new SimulationInstant(scenario.ExpiresAtMilliseconds),
                scenario.ReportedStatus.Value);

        return VerifyCredential(credential, scenario.ChallengeResponse, at);
    }

    public ContainmentShutterResult ExecuteShutter(
        ContainmentShutterCommandKind kind,
        SimulationInstant at,
        TransitArraySnapshot transit,
        string? reasonCode = null) =>
        _shutter.Execute(new ContainmentShutterCommand(kind, at, _latestAssessment, reasonCode), transit);

    public ReturnSecurityReadModel GetReadModel(TransitArraySnapshot transit)
    {
        ContainmentShutterState shutterState = _shutter.Snapshot.State;
        bool linkStable = transit.Phase == TransitArrayPhase.LinkOpen;
        bool canOpen = shutterState == ContainmentShutterState.Closed
            && linkStable
            && _latestAssessment?.IsAuthorized == true;

        (string warningCategory, string warningSummary) = ResolveWarning(_latestAssessment);

        return new ReturnSecurityReadModel(
            shutterState,
            _latestAssessment?.Status,
            _latestAssessment?.Outcome,
            _latestAssessment?.ReasonCode ?? "credential_not_checked",
            _latestAssessment?.CorrectiveAction ?? "Verify a Return Credential before changing containment.",
            canOpen,
            _latestAssessment?.Outcome == CredentialAuditOutcome.SecurityAlert,
            warningCategory,
            warningSummary,
            FormatShutter(shutterState),
            linkStable,
            shutterState == ContainmentShutterState.Opening,
            shutterState is ContainmentShutterState.Open or ContainmentShutterState.Opening,
            shutterState == ContainmentShutterState.Closing,
            shutterState != ContainmentShutterState.Faulted,
            shutterState == ContainmentShutterState.Faulted);
    }

    private static (string Category, string Summary) ResolveWarning(CredentialAssessment? assessment)
    {
        if (assessment is null)
        {
            return ("Unverified", "Return Credential has not been checked.");
        }

        return assessment.Outcome switch
        {
            CredentialAuditOutcome.Authorized => ("Clear", "Return Credential authorized."),
            CredentialAuditOutcome.Withheld => ("Rejected", assessment.CorrectiveAction),
            CredentialAuditOutcome.SecurityAlert => ("Dangerous", assessment.CorrectiveAction),
            _ => ("Unverified", assessment.CorrectiveAction),
        };
    }

    private static string FormatShutter(ContainmentShutterState state) => state switch
    {
        ContainmentShutterState.Closed => "Closed / secured",
        ContainmentShutterState.Opening => "Opening",
        ContainmentShutterState.Open => "Open",
        ContainmentShutterState.Closing => "Closing",
        ContainmentShutterState.Faulted => "Faulted / secured",
        _ => state.ToString(),
    };
}
