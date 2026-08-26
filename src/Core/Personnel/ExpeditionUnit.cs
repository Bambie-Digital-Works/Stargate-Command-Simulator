using FacilityCommand.Core.Transit;

namespace FacilityCommand.Core.Personnel;

public sealed class ExpeditionUnit
{
    public static readonly IReadOnlyList<string> RequiredEquipment =
    [
        "medkit",
        "comm_pack",
        "survey_kit",
        "sidearm",
    ];

    private static readonly PersonnelSpecialty[] RequiredSpecialties =
    [
        PersonnelSpecialty.Commander,
        PersonnelSpecialty.Medic,
        PersonnelSpecialty.Engineer,
        PersonnelSpecialty.Security,
    ];

    private readonly IReadOnlyDictionary<string, PersonnelMember> _pool;
    private ExpeditionUnitSnapshot _snapshot;

    public ExpeditionUnit(
        string unitId,
        string displayName,
        IEnumerable<PersonnelMember> pool)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(unitId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        Dictionary<string, PersonnelMember> members = new(StringComparer.Ordinal);
        foreach (PersonnelMember member in pool)
        {
            if (!members.TryAdd(member.Id, member))
            {
                throw new ArgumentException($"Personnel ID is duplicated: {member.Id}", nameof(pool));
            }
        }

        if (members.Count == 0)
        {
            throw new ArgumentException("At least one personnel member is required.", nameof(pool));
        }

        _pool = members;
        _snapshot = new ExpeditionUnitSnapshot(unitId, displayName, ExpeditionDispatchState.Standby, [], []);
    }

    public ExpeditionUnitSnapshot Snapshot => _snapshot;

    public IReadOnlyCollection<PersonnelMember> Pool => _pool.Values.ToArray();

    public void Restore(ExpeditionUnitSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(snapshot.UnitId, _snapshot.UnitId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Snapshot unit ID does not match this Expedition Unit.", nameof(snapshot));
        }

        foreach (string memberId in snapshot.AssignedMemberIds)
        {
            if (!_pool.ContainsKey(memberId))
            {
                throw new ArgumentException($"Snapshot references unknown personnel: {memberId}", nameof(snapshot));
            }
        }

        _snapshot = snapshot with
        {
            AssignedMemberIds = snapshot.AssignedMemberIds.ToArray(),
            EquippedItemIds = snapshot.EquippedItemIds.ToArray(),
        };
    }

    public ExpeditionOperationResult Assemble(IReadOnlyList<string> memberIds)
    {
        if (_snapshot.State is not (ExpeditionDispatchState.Standby or ExpeditionDispatchState.Recalled))
        {
            return Reject("invalid_roster_transition", "Return the Expedition Unit to Standby before assembling a new roster.");
        }

        if (memberIds.Count == 0)
        {
            return Reject("roster_empty", "Assign at least one available staff member to the Expedition Unit.");
        }

        List<PersonnelMember> assigned = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string memberId in memberIds)
        {
            if (!seen.Add(memberId))
            {
                return Reject("duplicate_assignment", "Each staff member may be assigned only once.");
            }

            if (!_pool.TryGetValue(memberId, out PersonnelMember? member))
            {
                return Reject("personnel_unknown", "Select staff recorded in the Expedition Roster pool.");
            }

            if (!member.IsAvailable)
            {
                return Reject("personnel_unavailable", $"{member.DisplayName} is unavailable for dispatch.");
            }

            if (member.IsInjured)
            {
                return Reject("personnel_injured", $"{member.DisplayName} is injured and cannot be assigned.");
            }

            if (member.Fatigue >= 80)
            {
                return Reject("personnel_fatigued", $"{member.DisplayName} is too fatigued for expedition duty.");
            }

            assigned.Add(member);
        }

        foreach (PersonnelSpecialty specialty in RequiredSpecialties)
        {
            if (assigned.All(member => member.Specialty != specialty))
            {
                string code = $"missing_specialty_{SpecialtyId(specialty)}";
                return Reject(code, $"Assign a {specialty} before assembling the Expedition Unit.");
            }
        }

        _snapshot = _snapshot with
        {
            State = ExpeditionDispatchState.Assembled,
            AssignedMemberIds = assigned.Select(member => member.Id).ToArray(),
            EquippedItemIds = [],
        };
        return Accept();
    }

    public ExpeditionOperationResult Equip(IReadOnlyList<string> itemIds)
    {
        if (_snapshot.State != ExpeditionDispatchState.Assembled)
        {
            return Reject("invalid_roster_transition", "Assemble a valid Expedition Unit before equipping it.");
        }

        HashSet<string> equipped = new(StringComparer.Ordinal);
        foreach (string itemId in itemIds)
        {
            if (!RequiredEquipment.Contains(itemId, StringComparer.Ordinal))
            {
                return Reject("equipment_unknown", "Select only approved expedition kit items.");
            }

            if (!equipped.Add(itemId))
            {
                return Reject("equipment_duplicate", "Each kit item may be issued only once.");
            }
        }

        foreach (string required in RequiredEquipment)
        {
            if (!equipped.Contains(required))
            {
                return Reject($"missing_equipment_{required}", $"Issue {required} before marking the Expedition Unit equipped.");
            }
        }

        _snapshot = _snapshot with
        {
            State = ExpeditionDispatchState.Equipped,
            EquippedItemIds = equipped.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
        };
        return Accept();
    }

    public ExpeditionOperationResult Dispatch(TransitArraySnapshot transit, bool riskDecisionRecorded)
    {
        if (_snapshot.State != ExpeditionDispatchState.Equipped)
        {
            return Reject("invalid_roster_transition", "Equip the Expedition Unit before dispatch.");
        }

        if (transit.Phase != TransitArrayPhase.LinkOpen)
        {
            return Reject("link_not_stable", "Wait for an active stable Transit Link before dispatching the Expedition Unit.");
        }

        if (!riskDecisionRecorded)
        {
            return Reject(
                "risk_decision_required",
                "Record a Survey Telemetry risk decision before dispatching the Expedition Unit.");
        }

        _snapshot = _snapshot with { State = ExpeditionDispatchState.Dispatched };
        return Accept();
    }

    public ExpeditionOperationResult Recall()
    {
        if (_snapshot.State != ExpeditionDispatchState.Dispatched)
        {
            return Reject("invalid_roster_transition", "Recall is available only while the Expedition Unit is dispatched.");
        }

        _snapshot = _snapshot with
        {
            State = ExpeditionDispatchState.Standby,
            AssignedMemberIds = [],
            EquippedItemIds = [],
        };
        return Accept();
    }

    private ExpeditionOperationResult Accept() => new(_snapshot, null);

    private ExpeditionOperationResult Reject(string reasonCode, string correctiveAction) =>
        new(_snapshot, new ExpeditionRejection(reasonCode, correctiveAction));

    private static string SpecialtyId(PersonnelSpecialty specialty) => specialty switch
    {
        PersonnelSpecialty.Commander => "commander",
        PersonnelSpecialty.Medic => "medic",
        PersonnelSpecialty.Engineer => "engineer",
        PersonnelSpecialty.Security => "security",
        _ => specialty.ToString().ToLowerInvariant(),
    };
}
