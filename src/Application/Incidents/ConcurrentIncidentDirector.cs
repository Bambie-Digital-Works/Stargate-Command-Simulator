using WormholeWorlds.Application.Simulation;

namespace WormholeWorlds.Application.Incidents;

public sealed record ScheduledIncident(
    string Id,
    string Title,
    string Location,
    long ActivatesAtMilliseconds,
    long DeadlineMilliseconds,
    int Priority);

public sealed record IncidentDirectorReadModel(
    IReadOnlyList<ScheduledIncident> ActiveIncidents,
    IReadOnlyList<string> ExpiredIncidentIds,
    string PrioritySummary);

public sealed class ConcurrentIncidentDirector
{
    private readonly ISimulationClock _clock;
    private readonly Dictionary<string, ScheduledIncident> _active = new(StringComparer.Ordinal);
    private readonly List<string> _expired = [];

    public ConcurrentIncidentDirector(ISimulationClock clock)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public bool Schedule(ScheduledIncident incident)
    {
        if (string.IsNullOrWhiteSpace(incident.Id)
            || string.IsNullOrWhiteSpace(incident.Title)
            || incident.DeadlineMilliseconds < incident.ActivatesAtMilliseconds
            || incident.Priority < 0
            || _active.ContainsKey(incident.Id))
        {
            return false;
        }

        _active.Add(incident.Id, incident);
        return true;
    }

    public bool Resolve(string incidentId) => _active.Remove(incidentId);

    public IncidentDirectorReadModel GetReadModel()
    {
        ExpireDueIncidents();
        ScheduledIncident[] active = _active.Values
            .Where(item => item.ActivatesAtMilliseconds <= _clock.Current.Milliseconds)
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.DeadlineMilliseconds)
            .ToArray();
        string summary = active.Length == 0
            ? "No concurrent incidents require attention."
            : $"Priority incident: {active[0].Title} ({active[0].Location}), {active.Length} active.";
        return new IncidentDirectorReadModel(active, _expired.ToArray(), summary);
    }

    private void ExpireDueIncidents()
    {
        long now = _clock.Current.Milliseconds;
        foreach ((string id, ScheduledIncident incident) in _active.ToArray())
        {
            if (incident.DeadlineMilliseconds > now)
            {
                continue;
            }

            _active.Remove(id);
            _expired.Add(id);
        }
    }
}
