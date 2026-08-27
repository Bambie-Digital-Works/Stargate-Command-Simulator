namespace WormholeWorlds.Core.Personnel;

public sealed record PersonnelMember(
    string Id,
    string DisplayName,
    PersonnelSpecialty Specialty,
    int Fatigue,
    bool IsInjured,
    bool IsAvailable);
