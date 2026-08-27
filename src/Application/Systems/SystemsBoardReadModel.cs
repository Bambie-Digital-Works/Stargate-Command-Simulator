namespace WormholeWorlds.Application.Systems;

public sealed record SystemsBoardReadModel(
    int PowerCapacity,
    int ReservedPower,
    int FreePower,
    int CoolingCapacity,
    int ReservedCooling,
    int CoolingFaultHold,
    int FreeCooling,
    bool HasCoolingFault,
    string CoolingStatus,
    string RepairGuidance,
    IReadOnlyList<string> AlarmHistory);
