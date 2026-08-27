using System.Text.RegularExpressions;

namespace WormholeWorlds.Application.Simulation;

public sealed partial class SeededIncidentSelector
{
    private readonly ISeededRandomSource _random;

    public SeededIncidentSelector(ISeededRandomSource random)
    {
        _random = random;
    }

    public string Select(IReadOnlyList<string> incidentIds)
    {
        if (incidentIds.Count == 0 || incidentIds.Any(id => !StableIdPattern().IsMatch(id)))
        {
            throw new ArgumentException("Incident IDs must be non-empty stable identifiers.", nameof(incidentIds));
        }

        return incidentIds[_random.NextInt(incidentIds.Count)];
    }

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();
}
