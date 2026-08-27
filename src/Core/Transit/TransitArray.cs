namespace WormholeWorlds.Core.Transit;

public sealed class TransitArray
{
    private const string NormalOperation = "normal_operation";
    private TransitArraySnapshot _snapshot = new(
        TransitArrayPhase.Standby,
        null,
        SimulationInstant.Zero,
        0,
        NormalOperation);

    public TransitArraySnapshot Snapshot => _snapshot;

    public TransitTransitionResult Execute(TransitArrayCommand command)
    {
        if (command.At.Milliseconds < _snapshot.EnteredAt.Milliseconds)
        {
            return Reject("non_monotonic_time", "Advance simulation time to the current phase timestamp or later.");
        }

        if (command.Kind == TransitArrayCommandKind.ReportFault && string.IsNullOrWhiteSpace(command.ReasonCode))
        {
            return Reject("missing_fault_reason", "Provide a stable fault reason code before reporting the fault.");
        }

        TransitArrayPhase? nextPhase = ResolveNextPhase(_snapshot.Phase, command.Kind);
        if (nextPhase is null)
        {
            return Reject(
                "invalid_transition",
                $"Wait for a command permitted while the Transit Array is {_snapshot.Phase}.");
        }

        TransitLinkDirection? direction = ResolveDirection(command.Kind, nextPhase.Value);
        string reasonCode = ResolveReasonCode(command);
        long sequence = _snapshot.LastEventSequence + 1;
        TransitArrayEvent transitEvent = new(
            sequence,
            command.At,
            ResolveEventKind(command.Kind),
            command.Kind,
            _snapshot.Phase,
            nextPhase.Value,
            direction,
            reasonCode);

        _snapshot = new TransitArraySnapshot(nextPhase.Value, direction, command.At, sequence, reasonCode);
        return new TransitTransitionResult(_snapshot, transitEvent, null);
    }

    private static TransitArrayPhase? ResolveNextPhase(TransitArrayPhase phase, TransitArrayCommandKind command) =>
        (phase, command) switch
        {
            (TransitArrayPhase.Standby, TransitArrayCommandKind.PrepareOutgoing) => TransitArrayPhase.OutgoingPreparation,
            (TransitArrayPhase.Standby, TransitArrayCommandKind.DetectIncoming) => TransitArrayPhase.IncomingDetected,

            (TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.BeginSequence) => TransitArrayPhase.Sequencing,
            (TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.BeginStabilization) => TransitArrayPhase.Stabilizing,
            (TransitArrayPhase.Sequencing, TransitArrayCommandKind.BeginStabilization) => TransitArrayPhase.Stabilizing,
            (TransitArrayPhase.Stabilizing, TransitArrayCommandKind.ConfirmStable) => TransitArrayPhase.LinkOpen,
            (TransitArrayPhase.LinkOpen, TransitArrayCommandKind.CloseLink) => TransitArrayPhase.Closing,
            (TransitArrayPhase.Closing, TransitArrayCommandKind.CompleteClosure) => TransitArrayPhase.Cooldown,
            (TransitArrayPhase.Cooldown, TransitArrayCommandKind.CompleteCooldown) => TransitArrayPhase.Standby,
            (TransitArrayPhase.Recovering, TransitArrayCommandKind.CompleteRecovery) => TransitArrayPhase.Standby,
            (TransitArrayPhase.Faulted, TransitArrayCommandKind.ResetFault) => TransitArrayPhase.Recovering,

            (TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.Abort) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.Abort) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Sequencing, TransitArrayCommandKind.Abort) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Stabilizing, TransitArrayCommandKind.Abort) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.LinkOpen, TransitArrayCommandKind.Abort) => TransitArrayPhase.Closing,

            (TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Sequencing, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Stabilizing, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.LinkOpen, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Closing,
            (TransitArrayPhase.Closing, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Cooldown, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Recovering,
            (TransitArrayPhase.Recovering, TransitArrayCommandKind.Timeout) => TransitArrayPhase.Faulted,

            (TransitArrayPhase.Standby, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.OutgoingPreparation, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.IncomingDetected, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.Sequencing, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.Stabilizing, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.LinkOpen, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.Closing, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.Cooldown, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            (TransitArrayPhase.Recovering, TransitArrayCommandKind.ReportFault) => TransitArrayPhase.Faulted,
            _ => null,
        };

    private TransitLinkDirection? ResolveDirection(TransitArrayCommandKind command, TransitArrayPhase nextPhase)
    {
        if (nextPhase == TransitArrayPhase.Standby)
        {
            return null;
        }

        return command switch
        {
            TransitArrayCommandKind.PrepareOutgoing => TransitLinkDirection.Outgoing,
            TransitArrayCommandKind.DetectIncoming => TransitLinkDirection.Incoming,
            _ => _snapshot.Direction,
        };
    }

    private static TransitArrayEventKind ResolveEventKind(TransitArrayCommandKind command) => command switch
    {
        TransitArrayCommandKind.Abort => TransitArrayEventKind.AbortInitiated,
        TransitArrayCommandKind.Timeout => TransitArrayEventKind.TimeoutInitiated,
        TransitArrayCommandKind.ReportFault => TransitArrayEventKind.FaultEntered,
        TransitArrayCommandKind.ResetFault => TransitArrayEventKind.FaultResetInitiated,
        _ => TransitArrayEventKind.PhaseChanged,
    };

    private static string ResolveReasonCode(TransitArrayCommand command) => command.Kind switch
    {
        TransitArrayCommandKind.Abort => command.ReasonCode ?? "operator_abort",
        TransitArrayCommandKind.Timeout => command.ReasonCode ?? "phase_timeout",
        TransitArrayCommandKind.ReportFault => command.ReasonCode!,
        TransitArrayCommandKind.ResetFault => command.ReasonCode ?? "fault_reset",
        _ => NormalOperation,
    };

    private TransitTransitionResult Reject(string reasonCode, string correctiveAction) =>
        new(_snapshot, null, new TransitTransitionRejection(reasonCode, correctiveAction));
}
