using WormholeWorlds.Core.Missions;

namespace WormholeWorlds.Application.Missions;

public sealed record MissionReadModel(
    string? ActiveMissionId,
    string ActiveMissionLabel,
    string Objective,
    string DestinationId,
    MissionPhase Phase,
    string PhaseLabel,
    int ProgressPercent,
    string StatusSummary,
    IReadOnlyList<MissionDefinition> AvailableMissions);
