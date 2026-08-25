namespace FacilityCommand.Core.Transit;

public sealed record TransitTransitionResult(
    TransitArraySnapshot Snapshot,
    TransitArrayEvent? Event,
    TransitTransitionRejection? Rejection)
{
    public bool IsAccepted => Event is not null;
}
