using WormholeWorlds.Core.Shift;

namespace WormholeWorlds.Application.Shift;

public sealed record ShiftReviewReadModel(
    string OutcomeCategoryLabel,
    string CategoryRuleSummary,
    int SuccessFactCount,
    int FailureFactCount,
    int TotalFactCount,
    IReadOnlyList<string> ChronologicalFactLines,
    IReadOnlyList<string> ConsequenceLines,
    ShiftPhase Phase,
    bool CanBeginNextShift);
