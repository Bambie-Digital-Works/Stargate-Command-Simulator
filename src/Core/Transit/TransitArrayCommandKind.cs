namespace FacilityCommand.Core.Transit;

public enum TransitArrayCommandKind
{
    PrepareOutgoing,
    DetectIncoming,
    BeginSequence,
    BeginStabilization,
    ConfirmStable,
    CloseLink,
    Abort,
    Timeout,
    CompleteClosure,
    CompleteCooldown,
    CompleteRecovery,
    ReportFault,
    ResetFault,
}
