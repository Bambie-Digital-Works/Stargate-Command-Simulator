namespace FacilityCommand.Core.Security;

public sealed record ContainmentShutterResult(
    ContainmentShutterSnapshot Snapshot,
    ContainmentShutterEvent? Event,
    ContainmentShutterRejection? Rejection)
{
    public bool IsAccepted => Event is not null;
}
