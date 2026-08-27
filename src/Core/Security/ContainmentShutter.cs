using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Core.Security;

public sealed class ContainmentShutter
{
    private ContainmentShutterSnapshot _snapshot = new(ContainmentShutterState.Closed, SimulationInstant.Zero, 0, "secured");

    public ContainmentShutterSnapshot Snapshot => _snapshot;

    public ContainmentShutterResult Execute(ContainmentShutterCommand command, TransitArraySnapshot transit)
    {
        if (command.At.Milliseconds < _snapshot.ChangedAt.Milliseconds)
        {
            return Reject("non_monotonic_time", "Advance simulation time to the latest shutter event or later.");
        }

        if (command.Kind == ContainmentShutterCommandKind.Open)
        {
            if (transit.Phase != TransitArrayPhase.LinkOpen)
            {
                return Reject("link_not_stable", "Wait for an active stable Transit Link before opening the Containment Shutter.");
            }

            if (command.Authorization?.IsAuthorized != true)
            {
                return Reject(command.Authorization?.ReasonCode ?? "authorization_missing", "Obtain a current verified Return Credential before opening the Containment Shutter.");
            }
        }

        ContainmentShutterState? next = ResolveNext(_snapshot.State, command.Kind);
        if (next is null)
        {
            return Reject("invalid_shutter_transition", $"Wait for a command permitted while the Containment Shutter is {_snapshot.State}.");
        }

        string reason = command.ReasonCode ?? ResolveReason(command.Kind);
        ContainmentShutterEvent shutterEvent = new(
            _snapshot.LastEventSequence + 1,
            command.At,
            command.Kind,
            _snapshot.State,
            next.Value,
            reason);
        _snapshot = new ContainmentShutterSnapshot(next.Value, command.At, shutterEvent.Sequence, reason);
        return new ContainmentShutterResult(_snapshot, shutterEvent, null);
    }

    private static ContainmentShutterState? ResolveNext(ContainmentShutterState state, ContainmentShutterCommandKind command) =>
        (state, command) switch
        {
            (ContainmentShutterState.Closed, ContainmentShutterCommandKind.Open) => ContainmentShutterState.Opening,
            (ContainmentShutterState.Opening, ContainmentShutterCommandKind.ConfirmOpened) => ContainmentShutterState.Open,
            (ContainmentShutterState.Open, ContainmentShutterCommandKind.Close) => ContainmentShutterState.Closing,
            (ContainmentShutterState.Opening, ContainmentShutterCommandKind.Close) => ContainmentShutterState.Closing,
            (ContainmentShutterState.Closing, ContainmentShutterCommandKind.ConfirmClosed) => ContainmentShutterState.Closed,
            (ContainmentShutterState.Faulted, ContainmentShutterCommandKind.ResetFault) => ContainmentShutterState.Closed,
            (_, ContainmentShutterCommandKind.ReportFault) when state != ContainmentShutterState.Faulted => ContainmentShutterState.Faulted,
            _ => null,
        };

    private static string ResolveReason(ContainmentShutterCommandKind command) => command switch
    {
        ContainmentShutterCommandKind.ReportFault => "shutter_fault",
        ContainmentShutterCommandKind.ResetFault => "safe_reset",
        _ => "operator_command",
    };

    private ContainmentShutterResult Reject(string reasonCode, string correctiveAction) =>
        new(_snapshot, null, new ContainmentShutterRejection(reasonCode, correctiveAction));
}
