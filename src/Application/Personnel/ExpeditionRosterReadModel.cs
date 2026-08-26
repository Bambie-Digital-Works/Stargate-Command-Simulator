using FacilityCommand.Core.Personnel;

namespace FacilityCommand.Application.Personnel;

public sealed record ExpeditionRosterReadModel(
    string UnitId,
    string UnitDisplayName,
    ExpeditionDispatchState State,
    string StateLabel,
    string Summary,
    string MissionClockDisplay,
    IReadOnlyList<PersonnelOption> Personnel,
    IReadOnlyList<string> AssignedMemberLabels,
    IReadOnlyList<string> EquippedItemIds,
    IReadOnlyList<string> RequiredEquipment,
    bool LinkIsStable,
    bool CanAssemble,
    bool CanEquip,
    bool CanDispatch,
    bool CanRecall,
    string StatusMessage);
