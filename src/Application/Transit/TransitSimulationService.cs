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

        return new TransitControlReadModel(
            phase,
            snapshot.DestinationId,
            snapshot.Vector,
            snapshot.LockedElements,
            snapshot.ReservedPower,
            snapshot.ReservedCooling,
            phase == TransitArrayPhase.OutgoingPreparation,
            phase == TransitArrayPhase.Sequencing && snapshot.LockedElements < snapshot.Vector.Count,
            phase == TransitArrayPhase.Sequencing && snapshot.Vector.Count > 0 && snapshot.LockedElements == snapshot.Vector.Count,
            phase is TransitArrayPhase.OutgoingPreparation or TransitArrayPhase.Sequencing or TransitArrayPhase.Stabilizing or TransitArrayPhase.LinkOpen,
            FormatPhase(phase),
            nextLock,
            _resources.PowerCapacity - snapshot.ReservedPower,
            _resources.CoolingCapacity - snapshot.ReservedCooling,
            _resources.PowerCapacity,
            _resources.CoolingCapacity,
            phase == TransitArrayPhase.Standby,
            phase == TransitArrayPhase.Stabilizing,
            phase == TransitArrayPhase.Recovering,
            phase == TransitArrayPhase.Closing,
            phase == TransitArrayPhase.Cooldown,
            FormatProgress(snapshot, nextLock));
    }

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
        if (snapshot.Vector.Count == 0)
        {
            return "Select a destination and prepare the Link Sequence.";
        }

        string destination = snapshot.DestinationId ?? "unknown destination";
        string locks = $"{snapshot.LockedElements}/{snapshot.Vector.Count} Vector Locks";
        return nextLock is null
            ? $"{destination}: {locks}."
            : $"{destination}: {locks}. Next lock: {nextLock}.";
    }
}
