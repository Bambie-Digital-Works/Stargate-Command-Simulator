namespace FacilityCommand.Core.Personnel;

public sealed record ExpeditionOperationResult(
    ExpeditionUnitSnapshot Snapshot,
    ExpeditionRejection? Rejection)
{
    public bool IsAccepted => Rejection is null;
}
