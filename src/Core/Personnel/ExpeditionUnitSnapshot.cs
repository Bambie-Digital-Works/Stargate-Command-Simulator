namespace FacilityCommand.Core.Personnel;

public sealed record ExpeditionUnitSnapshot(
    string UnitId,
    string DisplayName,
    ExpeditionDispatchState State,
    IReadOnlyList<string> AssignedMemberIds,
    IReadOnlyList<string> EquippedItemIds);
