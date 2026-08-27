namespace WormholeWorlds.Application.Personnel;

public sealed record PersonnelOption(
    string Id,
    string DisplayName,
    string SpecialtyLabel,
    int Fatigue,
    bool IsInjured,
    bool IsAvailable,
    bool IsAssigned);
