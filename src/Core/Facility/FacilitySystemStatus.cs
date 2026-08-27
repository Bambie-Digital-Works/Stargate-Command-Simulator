namespace WormholeWorlds.Core.Facility;

public sealed record FacilitySystemStatus(
    string Id,
    string DisplayName,
    int HealthPercent,
    bool Faulted,
    int RepairStepsRemaining);
