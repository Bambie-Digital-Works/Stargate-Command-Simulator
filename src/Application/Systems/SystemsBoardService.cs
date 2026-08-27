using WormholeWorlds.Application.Incidents;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Core.Incidents;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Systems;

public sealed class SystemsBoardService
{
    private const int HistoryLimit = 20;
    private readonly FacilityResourcePool _resources;
    private readonly ShiftIncidentService _incidents;
    private readonly List<string> _alarmHistory = [];
    private readonly Dictionary<string, string> _activeAlarmEntries = new(StringComparer.Ordinal);

    public SystemsBoardService(FacilityResourcePool resources, ShiftIncidentService incidents)
    {
        _resources = resources;
        _incidents = incidents;
    }

    public SystemsBoardReadModel GetReadModel() => new(
        _resources.PowerCapacity,
        _resources.ReservedPower,
        _resources.FreePower,
        _resources.CoolingCapacity,
        _resources.ReservedCooling,
        _resources.CoolingFaultHold,
        _resources.FreeCooling,
        _resources.CoolingFaultHold > 0,
        _resources.CoolingFaultHold > 0
            ? $"FAULT — {_resources.CoolingFaultHold} cooling units isolated"
            : "Nominal",
        _resources.CoolingFaultHold > 0
            ? "Run cooling bypass diagnostics, then clear the isolated fault hold."
            : "No repair action required.",
        _alarmHistory.ToArray());

    public IncidentOperationResult ResolveCoolingFault() => _incidents.ResolveCoolingFault();

    public void RecordActiveAlarms(IEnumerable<OperationsAlarm> alarms)
    {
        Dictionary<string, string> currentAlarmEntries = new(StringComparer.Ordinal);
        foreach (OperationsAlarm alarm in alarms)
        {
            string entry = $"[{alarm.SeverityLabel}] {alarm.Code}: {alarm.Message}";
            currentAlarmEntries[alarm.Code] = entry;
            if ((_activeAlarmEntries.TryGetValue(alarm.Code, out string? previousEntry)
                    && string.Equals(previousEntry, entry, StringComparison.Ordinal))
                || _alarmHistory.Count > 0 && string.Equals(_alarmHistory[^1], entry, StringComparison.Ordinal))
            {
                continue;
            }

            _alarmHistory.Add(entry);
            if (_alarmHistory.Count > HistoryLimit)
            {
                _alarmHistory.RemoveAt(0);
            }
        }

        _activeAlarmEntries.Clear();
        foreach ((string code, string entry) in currentAlarmEntries)
        {
            _activeAlarmEntries[code] = entry;
        }
    }
}
