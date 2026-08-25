namespace FacilityCommand.Core.Transit;

public sealed record OutgoingOperationResult(
    OutgoingConnectionSnapshot Snapshot,
    TransitTransitionRejection? Rejection)
{
    public bool IsAccepted => Rejection is null;
}
