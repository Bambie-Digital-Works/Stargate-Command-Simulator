using FacilityCommand.Core.Destinations;

namespace FacilityCommand.Core.Transit;

public sealed class OutgoingConnection
{
    private readonly TransitArray _transitArray = new();
    private readonly DestinationRegistry _registry;
    private readonly FacilityResourcePool _resources;
    private DestinationRecord? _destination;
    private int _lockedElements;
    private bool _hasReservation;

    public OutgoingConnection(DestinationRegistry registry, FacilityResourcePool resources)
    {
        _registry = registry;
        _resources = resources;
    }

    public OutgoingConnectionSnapshot Snapshot => new(
        _transitArray.Snapshot,
        _destination?.Id,
        _destination?.Vector ?? [],
        _lockedElements,
        _resources.ReservedPower,
        _resources.ReservedCooling);

    public OutgoingOperationResult Prepare(string destinationId, IReadOnlyList<string> vector, SimulationInstant at)
    {
        if (!_registry.TryGet(destinationId, out DestinationRecord? destination) || destination is null)
        {
            return Reject("destination_unknown", "Select a destination recorded in the Destination Registry.");
        }

        if (!destination.Vector.SequenceEqual(vector, StringComparer.Ordinal))
        {
            return Reject("destination_vector_invalid", "Enter the complete registered Destination Vector in order.");
        }

        if (!_resources.TryReserve(destination.RequiredPowerUnits, destination.RequiredCoolingUnits))
        {
            return Reject("resources_unavailable", "Free the required power and cooling capacity before preparing the Link Sequence.");
        }

        TransitTransitionResult transition = _transitArray.Execute(
            new TransitArrayCommand(TransitArrayCommandKind.PrepareOutgoing, at));
        if (!transition.IsAccepted)
        {
            _resources.Release(destination.RequiredPowerUnits, destination.RequiredCoolingUnits);
            return new OutgoingOperationResult(Snapshot, transition.Rejection);
        }

        _destination = destination;
        _lockedElements = 0;
        _hasReservation = true;
        return Accept();
    }

    public OutgoingOperationResult BeginSequence(SimulationInstant at) => Execute(TransitArrayCommandKind.BeginSequence, at);

    public OutgoingOperationResult LockNext(string vectorElement)
    {
        if (_transitArray.Snapshot.Phase != TransitArrayPhase.Sequencing || _destination is null)
        {
            return Reject("lock_not_available", "Begin a valid Link Sequence before requesting a Vector Lock.");
        }

        if (_lockedElements >= _destination.Vector.Count)
        {
            return Reject("sequence_complete", "Begin stabilization; every vector element is already locked.");
        }

        if (!string.Equals(_destination.Vector[_lockedElements], vectorElement, StringComparison.Ordinal))
        {
            return Reject("vector_lock_out_of_order", "Lock the next registered vector element in sequence.");
        }

        _lockedElements++;
        return Accept();
    }

    public OutgoingOperationResult BeginStabilization(SimulationInstant at)
    {
        if (_destination is null || _lockedElements != _destination.Vector.Count)
        {
            return Reject("sequence_incomplete", "Complete every Vector Lock before stabilization.");
        }

        return Execute(TransitArrayCommandKind.BeginStabilization, at);
    }

    public OutgoingOperationResult ConfirmStable(SimulationInstant at)
    {
        if (!_hasReservation || _destination is null ||
            _resources.ReservedPower < _destination.RequiredPowerUnits ||
            _resources.ReservedCooling < _destination.RequiredCoolingUnits)
        {
            return Reject("resource_reservation_lost", "Restore the reserved power and cooling before confirming stability.");
        }

        return Execute(TransitArrayCommandKind.ConfirmStable, at);
    }

    public OutgoingOperationResult Close(SimulationInstant at) => Execute(TransitArrayCommandKind.CloseLink, at);

    public OutgoingOperationResult Abort(SimulationInstant at) => Execute(TransitArrayCommandKind.Abort, at);

    public OutgoingOperationResult Timeout(SimulationInstant at, string reasonCode) => Execute(TransitArrayCommandKind.Timeout, at, reasonCode);

    public OutgoingOperationResult ReportFault(SimulationInstant at, string reasonCode) => Execute(TransitArrayCommandKind.ReportFault, at, reasonCode);

    public OutgoingOperationResult CompleteClosure(SimulationInstant at) => Execute(TransitArrayCommandKind.CompleteClosure, at);

    public OutgoingOperationResult CompleteCooldown(SimulationInstant at) => Execute(TransitArrayCommandKind.CompleteCooldown, at);

    public OutgoingOperationResult CompleteRecovery(SimulationInstant at) => Execute(TransitArrayCommandKind.CompleteRecovery, at);

    private OutgoingOperationResult Execute(TransitArrayCommandKind command, SimulationInstant at, string? reasonCode = null)
    {
        TransitTransitionResult result = _transitArray.Execute(new TransitArrayCommand(command, at, reasonCode));
        if (!result.IsAccepted)
        {
            return new OutgoingOperationResult(Snapshot, result.Rejection);
        }

        if (result.Snapshot.Phase is TransitArrayPhase.Recovering or TransitArrayPhase.Faulted or TransitArrayPhase.Cooldown or TransitArrayPhase.Standby)
        {
            ReleaseReservation();
        }

        if (result.Snapshot.Phase == TransitArrayPhase.Standby)
        {
            _destination = null;
            _lockedElements = 0;
        }

        return Accept();
    }

    private void ReleaseReservation()
    {
        if (_hasReservation && _destination is not null)
        {
            _resources.Release(_destination.RequiredPowerUnits, _destination.RequiredCoolingUnits);
            _hasReservation = false;
        }
    }

    private OutgoingOperationResult Accept() => new(Snapshot, null);

    private OutgoingOperationResult Reject(string reasonCode, string action) =>
        new(Snapshot, new TransitTransitionRejection(reasonCode, action));
}
