using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Transit;

public sealed class TransitSimulationService
{
    private readonly OutgoingConnection _outgoing;

    public TransitSimulationService(OutgoingConnection outgoing)
    {
        _outgoing = outgoing;
    }

    public TransitArraySnapshot GetTransitArraySnapshot() => _outgoing.Snapshot.TransitArray;

    public TransitControlReadModel GetTransitControl()
    {
        OutgoingConnectionSnapshot snapshot = _outgoing.Snapshot;
        TransitArrayPhase phase = snapshot.TransitArray.Phase;
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
            phase is TransitArrayPhase.OutgoingPreparation or TransitArrayPhase.Sequencing or TransitArrayPhase.Stabilizing or TransitArrayPhase.LinkOpen);
    }

    public OutgoingOperationResult Prepare(string destinationId, IReadOnlyList<string> vector, SimulationInstant at) =>
        _outgoing.Prepare(destinationId, vector, at);

    public OutgoingOperationResult BeginSequence(SimulationInstant at) => _outgoing.BeginSequence(at);

    public OutgoingOperationResult LockNext(string vectorElement) => _outgoing.LockNext(vectorElement);

    public OutgoingOperationResult BeginStabilization(SimulationInstant at) => _outgoing.BeginStabilization(at);

    public OutgoingOperationResult ConfirmStable(SimulationInstant at) => _outgoing.ConfirmStable(at);

    public OutgoingOperationResult Abort(SimulationInstant at) => _outgoing.Abort(at);
}
