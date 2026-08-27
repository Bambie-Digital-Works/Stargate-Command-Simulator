namespace WormholeWorlds.Core.Missions;

public sealed class MissionCatalog
{
    private readonly IReadOnlyDictionary<string, MissionDefinition> _missions;

    public MissionCatalog(IEnumerable<MissionDefinition> missions)
    {
        ArgumentNullException.ThrowIfNull(missions);
        Dictionary<string, MissionDefinition> entries = new(StringComparer.Ordinal);
        foreach (MissionDefinition mission in missions)
        {
            if (string.IsNullOrWhiteSpace(mission.Id))
            {
                throw new ArgumentException("Mission IDs cannot be empty.", nameof(missions));
            }

            if (!entries.TryAdd(mission.Id, mission))
            {
                throw new ArgumentException($"Mission ID is duplicated: {mission.Id}", nameof(missions));
            }
        }

        if (entries.Count == 0)
        {
            throw new ArgumentException("At least one mission is required.", nameof(missions));
        }

        _missions = entries;
    }

    public IReadOnlyList<MissionDefinition> All =>
        _missions.Values.OrderBy(mission => mission.Id, StringComparer.Ordinal).ToArray();

    public bool TryGet(string missionId, out MissionDefinition? mission) =>
        _missions.TryGetValue(missionId, out mission);
}
