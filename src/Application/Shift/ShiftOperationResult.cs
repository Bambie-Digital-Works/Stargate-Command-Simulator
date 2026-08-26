using FacilityCommand.Core.Shift;

namespace FacilityCommand.Application.Shift;

public sealed record ShiftOperationResult(bool IsAccepted, string? ReasonCode, string? Message)
{
    public static ShiftOperationResult Accepted() => new(true, null, null);

    public static ShiftOperationResult Rejected(string reasonCode, string message) =>
        new(false, reasonCode, message);
}
