namespace FacilityCommand.Core.Incidents;

/// <summary>
/// Deterministic sequential director for the authored vertical-slice incident set.
/// Activation side effects live in Application; Core only tracks progress and debrief facts.
/// </summary>
public sealed class IncidentScript
{
    private readonly IncidentCatalog _catalog;
    private int _index;
    private IncidentRunState _activeState = IncidentRunState.Pending;
    private readonly List<string> _completed = [];
    private readonly List<string> _failed = [];
    private readonly List<IncidentDebriefFact> _facts = [];

    public IncidentScript(IncidentCatalog catalog)
    {
        _catalog = catalog;
        _activeState = IncidentRunState.Active;
        _index = 0;
    }

    public IncidentProgressSnapshot Snapshot => BuildSnapshot();

    public IncidentDefinition? ActiveDefinition =>
        _index >= 0 && _index < _catalog.Ordered.Count && _activeState is IncidentRunState.Active or IncidentRunState.Pending
            ? _catalog.Ordered[_index]
            : null;

    public bool IsSequenceComplete => _completed.Count + _failed.Count >= _catalog.Ordered.Count;

    public IncidentOperationResult Observe(string eventCode)
    {
        if (IsSequenceComplete || ActiveDefinition is null || _activeState != IncidentRunState.Active)
        {
            return Reject("incident_not_active", "No active incident is waiting for an operator event.");
        }

        IncidentDefinition active = ActiveDefinition;
        if (string.Equals(eventCode, active.SuccessEvent, StringComparison.Ordinal))
        {
            foreach (string summary in active.SuccessFacts)
            {
                _facts.Add(new IncidentDebriefFact($"{active.Id}_success", summary));
            }

            _completed.Add(active.Id);
            Advance();
            return Accept();
        }

        if (string.Equals(eventCode, active.FailureEvent, StringComparison.Ordinal))
        {
            foreach (string summary in active.FailureFacts)
            {
                _facts.Add(new IncidentDebriefFact($"{active.Id}_failure", summary));
            }

            _failed.Add(active.Id);
            Advance();
            return Accept();
        }

        return Reject(
            "incident_event_mismatch",
            $"Active incident '{active.Id}' expects '{active.SuccessEvent}' or '{active.FailureEvent}'.");
    }

    private void Advance()
    {
        _index++;
        if (_index >= _catalog.Ordered.Count)
        {
            _activeState = IncidentRunState.Resolved;
            return;
        }

        _activeState = IncidentRunState.Active;
    }

    private IncidentProgressSnapshot BuildSnapshot() => new(
        ActiveDefinition?.Id,
        IsSequenceComplete ? IncidentRunState.Resolved : _activeState,
        _completed.Count,
        _failed.Count,
        _catalog.Ordered.Count,
        _completed.ToArray(),
        _failed.ToArray(),
        _facts.ToArray());

    private IncidentOperationResult Accept() => new(BuildSnapshot(), null);

    private IncidentOperationResult Reject(string reasonCode, string correctiveAction) =>
        new(BuildSnapshot(), new IncidentRejection(reasonCode, correctiveAction));
}
