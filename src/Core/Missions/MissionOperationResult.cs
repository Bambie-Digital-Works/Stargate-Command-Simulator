namespace WormholeWorlds.Core.Missions;

public sealed record MissionOperationResult(
    MissionSnapshot Snapshot,
    string? RejectionCode,
    string? Guidance)
{
    public bool IsAccepted => RejectionCode is null;

    public static MissionOperationResult Accepted(MissionSnapshot snapshot) =>
        new(snapshot, null, null);

    public static MissionOperationResult Rejected(
        MissionSnapshot snapshot,
        string code,
        string guidance) =>
        new(snapshot, code, guidance);
}
