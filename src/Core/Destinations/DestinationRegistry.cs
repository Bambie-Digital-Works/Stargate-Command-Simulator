using System.Text.RegularExpressions;

namespace WormholeWorlds.Core.Destinations;

public sealed partial class DestinationRegistry
{
    private readonly IReadOnlyDictionary<string, DestinationRecord> _records;

    public DestinationRegistry(IEnumerable<DestinationRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        Dictionary<string, DestinationRecord> validated = new(StringComparer.Ordinal);
        foreach (DestinationRecord record in records)
        {
            Validate(record);
            if (!validated.TryAdd(record.Id, record))
            {
                throw new ArgumentException($"Destination ID is duplicated: {record.Id}", nameof(records));
            }
        }

        if (validated.Count == 0)
        {
            throw new ArgumentException("At least one destination is required.", nameof(records));
        }

        _records = validated;
    }

    public IReadOnlyCollection<DestinationRecord> Records => _records.Values.ToArray();

    public bool TryGet(string id, out DestinationRecord? record) => _records.TryGetValue(id, out record);

    private static void Validate(DestinationRecord record)
    {
        if (!StableIdPattern().IsMatch(record.Id))
        {
            throw new ArgumentException($"Destination ID is invalid: {record.Id}");
        }

        if (string.IsNullOrWhiteSpace(record.DisplayName) || record.Vector.Count is < 4 or > 8 ||
            record.Vector.Any(element => !StableIdPattern().IsMatch(element)) ||
            record.Vector.Distinct(StringComparer.Ordinal).Count() != record.Vector.Count)
        {
            throw new ArgumentException($"Destination record is invalid: {record.Id}");
        }

        if (record.RequiredPowerUnits <= 0 || record.RequiredCoolingUnits <= 0)
        {
            throw new ArgumentException($"Destination resource requirements are invalid: {record.Id}");
        }
    }

    [GeneratedRegex("^[a-z0-9]+(?:_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();
}
