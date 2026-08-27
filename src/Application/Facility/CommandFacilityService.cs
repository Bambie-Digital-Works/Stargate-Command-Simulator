using WormholeWorlds.Core.Facility;

namespace WormholeWorlds.Application.Facility;

public sealed class CommandFacilityService
{
    private readonly Dictionary<string, FacilitySystemStatus> _systems;
    private readonly Dictionary<string, FacilityZoneStatus> _zones;
    private readonly Dictionary<string, DestinationIntel> _intelligence;
    private readonly Dictionary<string, FactionStanding> _factions;
    private readonly List<string> _alerts = [];

    public CommandFacilityService(
        IEnumerable<FacilitySystemStatus> systems,
        IEnumerable<FacilityZoneStatus> zones,
        IEnumerable<DestinationIntel> intelligence,
        IEnumerable<FactionStanding> factions)
    {
        _systems = ToDictionary(systems, item => item.Id, "system");
        _zones = ToDictionary(zones, item => item.Id, "zone");
        _intelligence = ToDictionary(intelligence, item => item.DestinationId, "destination");
        _factions = ToDictionary(factions, item => item.FactionId, "faction");
    }

    public static CommandFacilityService CreateDefault() =>
        new(
        [
            new FacilitySystemStatus("transit_power", "Transit power bus", 100, false, 0),
            new FacilitySystemStatus("cooling_loop", "Primary cooling loop", 100, false, 0),
            new FacilitySystemStatus("medical_grid", "Medical and quarantine grid", 100, false, 0),
            new FacilitySystemStatus("security_grid", "Security sensor grid", 100, false, 0),
        ],
        [
            new FacilityZoneStatus("transit_chamber", "Transit Chamber", false, "operations"),
            new FacilityZoneStatus("infirmary", "Infirmary", false, "medical"),
            new FacilityZoneStatus("archive", "Intelligence Archive", false, "command"),
        ],
        [],
        [
            new FactionStanding("uncontacted", "Uncontacted communities", 0, "No formal contact"),
            new FactionStanding("relay_custodians", "Relay Custodians", 10, "Initial signal exchange"),
        ]);

    public CommandFacilityReadModel GetReadModel()
    {
        int faultCount = _systems.Values.Count(item => item.Faulted);
        int lockedZones = _zones.Values.Count(item => item.LockedDown);
        string summary = faultCount == 0 && lockedZones == 0
            ? $"Facility nominal — {_intelligence.Count} intelligence records, {_factions.Count} active relationships."
            : $"Facility attention required — {faultCount} system faults, {lockedZones} zones locked down.";
        return new CommandFacilityReadModel(
            _systems.Values.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray(),
            _zones.Values.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray(),
            _intelligence.Values.OrderBy(item => item.DestinationId, StringComparer.Ordinal).ToArray(),
            _factions.Values.OrderBy(item => item.FactionId, StringComparer.Ordinal).ToArray(),
            _alerts.ToArray(),
            summary);
    }

    public bool ReportFault(string systemId, string alert)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alert);
        if (!_systems.TryGetValue(systemId, out FacilitySystemStatus? system))
        {
            return false;
        }

        _systems[systemId] = system with
        {
            HealthPercent = Math.Max(0, system.HealthPercent - 20),
            Faulted = true,
            RepairStepsRemaining = Math.Max(1, system.RepairStepsRemaining),
        };
        AddAlert($"[FAULT] {system.DisplayName}: {alert}");
        return true;
    }

    public bool QueueRepair(string systemId)
    {
        if (!_systems.TryGetValue(systemId, out FacilitySystemStatus? system) || !system.Faulted)
        {
            return false;
        }

        _systems[systemId] = system with
        {
            RepairStepsRemaining = Math.Max(1, system.RepairStepsRemaining),
        };
        AddAlert($"[ENGINEERING] Repair queued: {system.DisplayName}.");
        return true;
    }

    public bool AdvanceRepairs(int steps = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(steps);
        bool changed = false;
        foreach ((string id, FacilitySystemStatus system) in _systems.ToArray())
        {
            if (!system.Faulted || system.RepairStepsRemaining <= 0)
            {
                continue;
            }

            int remaining = Math.Max(0, system.RepairStepsRemaining - steps);
            _systems[id] = system with
            {
                RepairStepsRemaining = remaining,
                Faulted = remaining > 0,
                HealthPercent = remaining > 0 ? system.HealthPercent : 100,
            };
            changed = true;
            if (remaining == 0)
            {
                AddAlert($"[ENGINEERING] Repair complete: {system.DisplayName}.");
            }
        }

        return changed;
    }

    public bool SetLockdown(string zoneId, bool lockedDown, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (!_zones.TryGetValue(zoneId, out FacilityZoneStatus? zone))
        {
            return false;
        }

        _zones[zoneId] = zone with { LockedDown = lockedDown };
        AddAlert(lockedDown
            ? $"[SECURITY] {zone.DisplayName} locked down: {reason}"
            : $"[SECURITY] {zone.DisplayName} reopened: {reason}");
        return true;
    }

    public bool RecordIntelligence(
        string destinationId,
        int confidencePercent,
        string report,
        IEnumerable<string> knownSignals)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(report);
        if (confidencePercent is < 0 or > 100)
        {
            return false;
        }

        _intelligence[destinationId] = new DestinationIntel(
            destinationId,
            confidencePercent,
            report,
            knownSignals.Distinct(StringComparer.Ordinal).OrderBy(item => item, StringComparer.Ordinal).ToArray());
        AddAlert($"[INTELLIGENCE] Updated destination record: {destinationId} ({confidencePercent}% confidence).");
        return true;
    }

    public bool AdjustFactionTrust(string factionId, int delta, string contactSummary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contactSummary);
        if (!_factions.TryGetValue(factionId, out FactionStanding? faction))
        {
            return false;
        }

        _factions[factionId] = faction with
        {
            Trust = Math.Clamp(faction.Trust + delta, -100, 100),
            LatestContact = contactSummary,
        };
        AddAlert($"[DIPLOMACY] {faction.DisplayName}: {contactSummary}");
        return true;
    }

    private void AddAlert(string alert)
    {
        _alerts.Add(alert);
        if (_alerts.Count > 30)
        {
            _alerts.RemoveAt(0);
        }
    }

    private static Dictionary<string, TValue> ToDictionary<TValue>(
        IEnumerable<TValue> values,
        Func<TValue, string> keySelector,
        string kind)
    {
        Dictionary<string, TValue> dictionary = new(StringComparer.Ordinal);
        foreach (TValue value in values)
        {
            string key = keySelector(value);
            if (string.IsNullOrWhiteSpace(key) || !dictionary.TryAdd(key, value))
            {
                throw new ArgumentException($"Duplicate or empty {kind} identifier: {key}");
            }
        }

        return dictionary;
    }
}
