using WormholeWorlds.Application.Security;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Tests;

public sealed class ReturnSecurityTests
{
    public static TheoryData<ReturnCredentialStatus, CredentialAuditOutcome, string> RejectedStatuses => new()
    {
        { ReturnCredentialStatus.Missing, CredentialAuditOutcome.Withheld, "credential_missing" },
        { ReturnCredentialStatus.Damaged, CredentialAuditOutcome.Withheld, "credential_damaged" },
        { ReturnCredentialStatus.Expired, CredentialAuditOutcome.Withheld, "credential_expired" },
        { ReturnCredentialStatus.Duress, CredentialAuditOutcome.SecurityAlert, "credential_duress" },
        { ReturnCredentialStatus.Duplicate, CredentialAuditOutcome.Withheld, "credential_duplicate" },
        { ReturnCredentialStatus.SpoofSuspected, CredentialAuditOutcome.SecurityAlert, "credential_spoof_suspected" },
    };

    [Fact]
    public void MatchingCurrentCredentialIsAuthorizedAndAudited()
    {
        ReturnCredentialVerifier verifier = new();
        ReturnCredential credential = Credential();

        CredentialVerificationResult result = verifier.Verify(credential, "proof-7", new SimulationInstant(10));

        Assert.True(result.Assessment.IsAuthorized);
        Assert.Equal(ReturnCredentialStatus.Verified, result.Assessment.Status);
        Assert.Equal(1, result.AuditEvent.Sequence);
        Assert.Equal("credential_verified", result.AuditEvent.ReasonCode);
    }

    [Theory]
    [MemberData(nameof(RejectedStatuses))]
    public void ReportedUnsafeStatusWithholdsAuthorization(
        ReturnCredentialStatus status,
        CredentialAuditOutcome expectedOutcome,
        string reasonCode)
    {
        ReturnCredential? credential = status == ReturnCredentialStatus.Missing
            ? null
            : Credential() with { ReportedStatus = status };
        ReturnCredentialVerifier verifier = new();

        CredentialVerificationResult result = verifier.Verify(credential, "proof-7", new SimulationInstant(10));

        Assert.False(result.Assessment.IsAuthorized);
        Assert.Equal(status, result.Assessment.Status);
        Assert.Equal(expectedOutcome, result.Assessment.Outcome);
        Assert.Equal(reasonCode, result.Assessment.ReasonCode);
    }

    [Fact]
    public void ExpiryIsDerivedFromSimulationTime()
    {
        ReturnCredentialVerifier verifier = new();

        CredentialVerificationResult result = verifier.Verify(Credential(), "proof-7", new SimulationInstant(101));

        Assert.Equal(ReturnCredentialStatus.Expired, result.Assessment.Status);
        Assert.False(result.Assessment.IsAuthorized);
    }

    [Fact]
    public void WrongChallengeIsReportedAsSpoofSuspected()
    {
        ReturnCredentialVerifier verifier = new();

        CredentialVerificationResult result = verifier.Verify(Credential(), "wrong", new SimulationInstant(10));

        Assert.Equal(ReturnCredentialStatus.SpoofSuspected, result.Assessment.Status);
        Assert.Equal(CredentialAuditOutcome.SecurityAlert, result.Assessment.Outcome);
    }

    [Fact]
    public void ReusingAcceptedCredentialIsRejectedAsDuplicate()
    {
        ReturnCredentialVerifier verifier = new();
        ReturnCredential credential = Credential();
        verifier.Verify(credential, "proof-7", new SimulationInstant(10));

        CredentialVerificationResult replay = verifier.Verify(credential, "proof-7", new SimulationInstant(11));

        Assert.Equal(ReturnCredentialStatus.Duplicate, replay.Assessment.Status);
        Assert.Equal(2, replay.AuditEvent.Sequence);
    }

    [Fact]
    public void ShutterOpensOnlyForStableLinkAndAuthorization()
    {
        ContainmentShutter shutter = new();
        CredentialAssessment authorization = Authorized();

        ContainmentShutterResult opening = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.Open, new SimulationInstant(10), authorization),
            Transit(TransitArrayPhase.LinkOpen));
        ContainmentShutterResult opened = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.ConfirmOpened, new SimulationInstant(11)),
            Transit(TransitArrayPhase.LinkOpen));

        Assert.True(opening.IsAccepted);
        Assert.Equal(ContainmentShutterState.Opening, opening.Snapshot.State);
        Assert.Equal(ContainmentShutterState.Open, opened.Snapshot.State);
    }

    [Fact]
    public void ShutterRejectsAuthorizedOpeningWithoutStableLink()
    {
        ContainmentShutter shutter = new();

        ContainmentShutterResult result = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.Open, new SimulationInstant(10), Authorized()),
            Transit(TransitArrayPhase.Stabilizing));

        Assert.False(result.IsAccepted);
        Assert.Equal("link_not_stable", result.Rejection?.ReasonCode);
        Assert.True(result.Snapshot.IsSecured);
    }

    [Theory]
    [MemberData(nameof(RejectedStatuses))]
    public void EveryUnsafeCredentialCannotOpenShutter(
        ReturnCredentialStatus status,
        CredentialAuditOutcome outcome,
        string reasonCode)
    {
        ContainmentShutter shutter = new();
        CredentialAssessment assessment = new(status, outcome, reasonCode, "Correct the credential.");

        ContainmentShutterResult result = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.Open, new SimulationInstant(10), assessment),
            Transit(TransitArrayPhase.LinkOpen));

        Assert.False(result.IsAccepted);
        Assert.Equal(reasonCode, result.Rejection?.ReasonCode);
        Assert.True(result.Snapshot.IsSecured);
    }

    [Fact]
    public void FaultFromOpenFailsSecureAndRequiresReset()
    {
        ContainmentShutter shutter = OpenShutter();

        ContainmentShutterResult fault = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.ReportFault, new SimulationInstant(12), ReasonCode: "drive_fault"),
            Transit(TransitArrayPhase.LinkOpen));
        ContainmentShutterResult rejectedOpen = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.Open, new SimulationInstant(13), Authorized()),
            Transit(TransitArrayPhase.LinkOpen));
        ContainmentShutterResult reset = shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.ResetFault, new SimulationInstant(14)),
            Transit(TransitArrayPhase.LinkOpen));

        Assert.Equal(ContainmentShutterState.Faulted, fault.Snapshot.State);
        Assert.True(fault.Snapshot.IsSecured);
        Assert.Equal("invalid_shutter_transition", rejectedOpen.Rejection?.ReasonCode);
        Assert.Equal(ContainmentShutterState.Closed, reset.Snapshot.State);
    }

    [Fact]
    public void ApplicationReadModelExposesInterlockAndAlertWithoutMutableState()
    {
        ReturnSecurityService service = new(new ReturnCredentialVerifier(), new ContainmentShutter());
        service.VerifyCredential(Credential() with { ReportedStatus = ReturnCredentialStatus.Duress }, "proof-7", new SimulationInstant(10));

        ReturnSecurityReadModel model = service.GetReadModel(Transit(TransitArrayPhase.LinkOpen));

        Assert.False(model.CanOpenShutter);
        Assert.True(model.HasSecurityAlert);
        Assert.Equal("credential_duress", model.StatusCode);
    }

    private static ReturnCredential Credential() => new("credential-7", "expedition-unit-7", "proof-7", new SimulationInstant(100));

    private static CredentialAssessment Authorized() => new(
        ReturnCredentialStatus.Verified,
        CredentialAuditOutcome.Authorized,
        "credential_verified",
        "Authorization is valid.");

    private static TransitArraySnapshot Transit(TransitArrayPhase phase) => new(
        phase,
        TransitLinkDirection.Incoming,
        SimulationInstant.Zero,
        0,
        "test");

    private static ContainmentShutter OpenShutter()
    {
        ContainmentShutter shutter = new();
        shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.Open, new SimulationInstant(10), Authorized()),
            Transit(TransitArrayPhase.LinkOpen));
        shutter.Execute(
            new ContainmentShutterCommand(ContainmentShutterCommandKind.ConfirmOpened, new SimulationInstant(11)),
            Transit(TransitArrayPhase.LinkOpen));
        return shutter;
    }
}
