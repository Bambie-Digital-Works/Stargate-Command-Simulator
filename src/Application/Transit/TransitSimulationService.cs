using FacilityCommand.Core.Destinations;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Transit;

public sealed class TransitSimulationService
{
    private readonly OutgoingConnection _outgoing;
    private readonly DestinationRegistry _destinations;
    private readonly FacilityResourcePool _resources;

    public TransitSimulationService(
        OutgoingConnection outgoing,
        DestinationRegistry destinations,
        FacilityResourcePool resources)
    {
        _outgoing = outgoing;
        _destinations = destinations;
        _resources = resources;
    }

    public TransitArraySnapshot GetTransitArraySnapshot() => _outgoing.Snapshot.TransitArray;

    public IReadOnlyList<DestinationOption> ListDestinations() =>
        _destinations.Records
            .OrderBy(record => record.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(record => new DestinationOption(
                record.Id,
                record.DisplayName,
                record.Vector,
                record.RequiredPowerUnits,
                record.RequiredCoolingUnits))
            .ToArray();

    public TransitControlReadModel GetTransitControl()
    {
        OutgoingConnectionSnapshot snapshot = _outgoing.Snapshot;
        TransitArrayPhase phase = snapshot.TransitArray.Phase;
        string? nextLock = phase == TransitArrayPhase.Sequencing
            && snapshot.LockedElements < snapshot.Vector.Count
            ? snapshot.Vector[snapshot.LockedElements]
            : null;

        bool canStabilize = phase == TransitArrayPhase.IncomingDetected
            || (phase == TransitArrayPhase.Sequencing
                && snapshot.Vector.Count > 0
                && snapshot.LockedElements == snapshot.Vector.Count);

        return new TransitControlReadModel(
            phase,
            snapshot.DestinationId,
            snapshot.Vector,
            snapshot.LockedElements,
            snapshot.ReservedPower,
            snapshot.ReservedCooling,
            phase == TransitArrayPhase.OutgoingPreparation,
            phase == TransitArrayPhase.Sequencing && snapshot.LockedElements < snapshot.Vector.Count,
            canStabilize,
            phase is TransitArrayPhase.OutgoingPreparation
                or TransitArrayPhase.IncomingDetected
                or TransitArrayPhase.Sequencing
                or TransitArrayPhase.Stabilizing
                or TransitArrayPhase.LinkOpen,
            FormatPhase(phase),
            nextLock,
            _resources.FreePower,
            _resources.FreeCooling,
            _resources.PowerCapacity,
            _resources.CoolingCapacity,
            phase == TransitArrayPhase.Standby,
            phase == TransitArrayPhase.Standby,
            phase == TransitArrayPhase.Stabilizing,
            phase == TransitArrayPhase.Recovering,
            phase == TransitArrayPhase.Closing,
            phase == TransitArrayPhase.Cooldown,
            FormatProgress(snapshot, nextLock));
    }

    public OutgoingOperationResult DetectIncoming(SimulationInstant at) => _outgoing.DetectIncoming(at);

    public OutgoingOperationResult PrepareSelected(string destinationId, SimulationInstant at)
    {
        if (!_destinations.TryGet(destinationId, out DestinationRecord? destination) || destination is null)
        {
            return _outgoing.Prepare(destinationId, [], at);
        }

        return _outgoing.Prepare(destination.Id, destination.Vector, at);
    }

    public OutgoingOperationResult Prepare(string destinationId, IReadOnlyList<string> vector, SimulationInstant at) =>
        _outgoing.Prepare(destinationId, vector, at);

    public OutgoingOperationResult BeginSequence(SimulationInstant at) => _outgoing.BeginSequence(at);

    public OutgoingOperationResult LockNext(string vectorElement) => _outgoing.LockNext(vectorElement);

    public OutgoingOperationResult LockNextExpected()
    {
        TransitControlReadModel model = GetTransitControl();
        if (model.NextLockElement is null)
        {
            return new OutgoingOperationResult(
                _outgoing.Snapshot,
                new TransitTransitionRejection(
                    "lock_not_available",
                    "Wait until the Link Sequence is ready for the next Vector Lock."));
        }

        return _outgoing.LockNext(model.NextLockElement);
    }

    public OutgoingOperationResult BeginStabilization(SimulationInstant at) => _outgoing.BeginStabilization(at);

    public OutgoingOperationResult ConfirmStable(SimulationInstant at) => _outgoing.ConfirmStable(at);

    public OutgoingOperationResult Abort(SimulationInstant at) => _outgoing.Abort(at);

    public OutgoingOperationResult CompleteRecovery(SimulationInstant at) => _outgoing.CompleteRecovery(at);

    public OutgoingOperationResult CompleteClosure(SimulationInstant at) => _outgoing.CompleteClosure(at);

    public OutgoingOperationResult CompleteCooldown(SimulationInstant at) => _outgoing.CompleteCooldown(at);

    public OutgoingOperationResult ReportFault(SimulationInstant at, string reasonCode) =>
        _outgoing.ReportFault(at, reasonCode);

    private static string FormatPhase(TransitArrayPhase phase) => phase switch
    {
        TransitArrayPhase.Standby => "Standby",
        TransitArrayPhase.OutgoingPreparation => "Outgoing preparation",
        TransitArrayPhase.IncomingDetected => "Incoming detected",
        TransitArrayPhase.Sequencing => "Sequencing",
        TransitArrayPhase.Stabilizing => "Stabilizing",
        TransitArrayPhase.LinkOpen => "Link open",
        TransitArrayPhase.Closing => "Closing",
        TransitArrayPhase.Cooldown => "Cooldown",
        TransitArrayPhase.Recovering => "Recovering",
        TransitArrayPhase.Faulted => "Faulted",
        _ => phase.ToString(),
    };

    private static string FormatProgress(OutgoingConnectionSnapshot snapshot, string? nextLock)
    {
        TransitArrayPhase phase = snapshot.TransitArray.Phase;
        if (phase == TransitArrayPhase.IncomingDetected)
        {
            return "Unscheduled incoming Transit Link detected. Begin stabilization (no Vector Lock sequence).";
        }

        if (phase == TransitArrayPhase.Standby)
        {
            return "Select a destination and prepare the Link Sequence, or detect an unscheduled incoming Transit Link.";
        }

        if (string.Equals(snapshot.DestinationId, OutgoingConnection.UnscheduledIncoming.Id, StringComparison.Ordinal))
        {
            return phase switch
            {
                TransitArrayPhase.Stabilizing => "Unscheduled incoming: stabilize and confirm before authentication.",
                TransitArrayPhase.LinkOpen => "Unscheduled incoming link is open. Authenticate at Return Control.",
                _ => "Unscheduled incoming Transit Link in progress.",
            };
        }

        if (snapshot.Vector.Count == 0)
        {
            return "Select a destination and prepare the Link Sequence, or detect an unscheduled incoming Transit Link.";
        }

        string destination = snapshot.DestinationId ?? "unknown destination";
        string locks = $"{snapshot.LockedElements}/{snapshot.Vector.Count} Vector Locks";
        return nextLock is null
            ? $"{destination}: {locks}."
            : $"{destination}: {locks}. Next lock: {nextLock}.";
    }
}
