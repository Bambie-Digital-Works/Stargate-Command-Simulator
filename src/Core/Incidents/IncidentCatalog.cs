namespace WormholeWorlds.Core.Incidents;

public sealed class IncidentCatalog
{
    private readonly IReadOnlyList<IncidentDefinition> _ordered;

    public IncidentCatalog(IEnumerable<IncidentDefinition> definitions)
    {
        _ordered = definitions
            .OrderBy(definition => definition.SequenceOrder)
            .ThenBy(definition => definition.Id, StringComparer.Ordinal)
            .ToArray();
        if (_ordered.Count == 0)
        {
            throw new ArgumentException("Incident catalog requires at least one definition.", nameof(definitions));
        }

        HashSet<string> ids = new(StringComparer.Ordinal);
        foreach (IncidentDefinition definition in _ordered)
        {
            if (!ids.Add(definition.Id))
            {
                throw new ArgumentException($"Duplicate incident id: {definition.Id}", nameof(definitions));
            }
        }
    }

    public IReadOnlyList<IncidentDefinition> Ordered => _ordered;

    public bool TryGet(string id, out IncidentDefinition? definition)
    {
        definition = _ordered.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        return definition is not null;
    }
}
