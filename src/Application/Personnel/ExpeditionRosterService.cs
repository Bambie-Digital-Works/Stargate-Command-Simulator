using System.Text.Json;
using FacilityCommand.Application.Simulation;
using FacilityCommand.Core.Personnel;
using FacilityCommand.Core.Transit;

namespace FacilityCommand.Application.Personnel;

public sealed class ExpeditionRosterService
{
    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ExpeditionUnit _unit;
    private readonly ISimulationClock _clock;

    public ExpeditionRosterService(ExpeditionUnit unit, ISimulationClock clock)
    {
        _unit = unit;
        _clock = clock;
    }

    public static ExpeditionRosterService CreateDefault(ISimulationClock clock) =>
        new(CreateDefaultUnit(), clock);

    public ExpeditionUnitSnapshot Snapshot => _unit.Snapshot;

    public IReadOnlyList<PersonnelOption> ListAvailablePersonnel() =>
        BuildPersonnelOptions(_unit.Snapshot);

    public ExpeditionOperationResult Assemble(IReadOnlyList<string> memberIds) => _unit.Assemble(memberIds);

    public ExpeditionOperationResult EquipRequiredKit() => _unit.Equip(ExpeditionUnit.RequiredEquipment);

    public ExpeditionOperationResult Equip(IReadOnlyList<string> itemIds) => _unit.Equip(itemIds);

    public ExpeditionOperationResult Dispatch(TransitArraySnapshot transit) => _unit.Dispatch(transit);

    public ExpeditionOperationResult Recall() => _unit.Recall();

    public void RestoreSnapshot(ExpeditionUnitSnapshot snapshot) => _unit.Restore(snapshot);

    public string ExportSnapshotJson() => JsonSerializer.Serialize(_unit.Snapshot, SnapshotJsonOptions);

    public void ImportSnapshotJson(string json)
    {
        ExpeditionUnitSnapshot? snapshot = JsonSerializer.Deserialize<ExpeditionUnitSnapshot>(json, SnapshotJsonOptions)
            ?? throw new InvalidDataException("Expedition Unit snapshot is empty.");
        _unit.Restore(snapshot);
    }

    public ExpeditionRosterReadModel GetReadModel(TransitArraySnapshot transit)
    {
        ExpeditionUnitSnapshot snapshot = _unit.Snapshot;
        bool linkStable = transit.Phase == TransitArrayPhase.LinkOpen;
        return new ExpeditionRosterReadModel(
            snapshot.UnitId,
            snapshot.DisplayName,
            snapshot.State,
            FormatState(snapshot.State),
            FormatSummary(snapshot),
            FormatMissionClock(_clock.Current.Milliseconds),
            BuildPersonnelOptions(snapshot),
            snapshot.AssignedMemberIds
                .Select(id => _unit.Pool.First(member => member.Id == id))
                .Select(member => $"{member.DisplayName} ({FormatSpecialty(member.Specialty)})")
                .ToArray(),
            snapshot.EquippedItemIds,
            ExpeditionUnit.RequiredEquipment,
            linkStable,
            snapshot.State is ExpeditionDispatchState.Standby or ExpeditionDispatchState.Recalled,
            snapshot.State == ExpeditionDispatchState.Assembled,
            snapshot.State == ExpeditionDispatchState.Equipped && linkStable,
            snapshot.State == ExpeditionDispatchState.Dispatched,
            FormatStatus(snapshot, linkStable));
    }

    private IReadOnlyList<PersonnelOption> BuildPersonnelOptions(ExpeditionUnitSnapshot snapshot)
    {
        HashSet<string> assigned = new(snapshot.AssignedMemberIds, StringComparer.Ordinal);
        return _unit.Pool
            .OrderBy(member => member.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(member => new PersonnelOption(
                member.Id,
                member.DisplayName,
                FormatSpecialty(member.Specialty),
                member.Fatigue,
                member.IsInjured,
                member.IsAvailable,
                assigned.Contains(member.Id)))
            .ToArray();
    }

    private static ExpeditionUnit CreateDefaultUnit() => new(
        "eu_alpha",
        "Expedition Unit Alpha",
        [
            new PersonnelMember("staff_harper", "Harper Quinn", PersonnelSpecialty.Commander, 12, false, true),
            new PersonnelMember("staff_okoye", "Amara Okoye", PersonnelSpecialty.Medic, 18, false, true),
            new PersonnelMember("staff_vesper", "Jonah Vesper", PersonnelSpecialty.Engineer, 22, false, true),
            new PersonnelMember("staff_reed", "Casey Reed", PersonnelSpecialty.Security, 15, false, true),
            new PersonnelMember("staff_nolan", "Ivy Nolan", PersonnelSpecialty.Security, 40, false, true),
            new PersonnelMember("staff_brook", "Theo Brook", PersonnelSpecialty.Medic, 85, false, true),
            new PersonnelMember("staff_kane", "Morgan Kane", PersonnelSpecialty.Engineer, 10, true, true),
        ]);

    private static string FormatSummary(ExpeditionUnitSnapshot snapshot) => snapshot.State switch
    {
        ExpeditionDispatchState.Standby => "No Expedition Unit assigned.",
        ExpeditionDispatchState.Assembled => $"{snapshot.DisplayName} — Assembled ({snapshot.AssignedMemberIds.Count})",
        ExpeditionDispatchState.Equipped => $"{snapshot.DisplayName} — Equipped ({snapshot.AssignedMemberIds.Count})",
        ExpeditionDispatchState.Dispatched => $"{snapshot.DisplayName} — Dispatched ({snapshot.AssignedMemberIds.Count})",
        ExpeditionDispatchState.Recalled => $"{snapshot.DisplayName} — Recalled",
        _ => $"{snapshot.DisplayName} — {snapshot.State}",
    };

    private static string FormatStatus(ExpeditionUnitSnapshot snapshot, bool linkStable) => snapshot.State switch
    {
        ExpeditionDispatchState.Standby => "Select available staff covering commander, medic, engineer, and security.",
        ExpeditionDispatchState.Assembled => "Issue the required kit before dispatch.",
        ExpeditionDispatchState.Equipped when !linkStable => "Transit Link is not stable. Open a stable link before dispatch.",
        ExpeditionDispatchState.Equipped => "Expedition Unit is ready to dispatch.",
        ExpeditionDispatchState.Dispatched => "Expedition Unit is in the field.",
        ExpeditionDispatchState.Recalled => "Expedition Unit recalled. Assemble a new roster when ready.",
        _ => "Expedition Roster ready.",
    };

    private static string FormatState(ExpeditionDispatchState state) => state switch
    {
        ExpeditionDispatchState.Standby => "Standby",
        ExpeditionDispatchState.Assembled => "Assembled",
        ExpeditionDispatchState.Equipped => "Equipped",
        ExpeditionDispatchState.Dispatched => "Dispatched",
        ExpeditionDispatchState.Recalled => "Recalled",
        _ => state.ToString(),
    };

    private static string FormatSpecialty(PersonnelSpecialty specialty) => specialty switch
    {
        PersonnelSpecialty.Commander => "Commander",
        PersonnelSpecialty.Medic => "Medic",
        PersonnelSpecialty.Engineer => "Engineer",
        PersonnelSpecialty.Security => "Security",
        _ => specialty.ToString(),
    };

    private static string FormatMissionClock(long milliseconds)
    {
        long totalSeconds = Math.Max(0, milliseconds) / 1000;
        long hours = totalSeconds / 3600;
        long minutes = (totalSeconds % 3600) / 60;
        long seconds = totalSeconds % 60;
        return hours > 0
            ? $"T+{hours}:{minutes:00}:{seconds:00}"
            : $"T+{minutes:00}:{seconds:00}";
    }
}
