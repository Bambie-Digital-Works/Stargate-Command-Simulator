namespace FacilityCommand.Core.Shift;

public sealed record ShiftScore(
    int SuccessFactCount,
    int FailureFactCount,
    int TotalFactCount,
    ShiftOutcomeCategory Category,
    string CategoryRuleSummary);
