namespace WormholeWorlds.Core.Incidents;

public sealed record IncidentRejection(string ReasonCode, string CorrectiveAction);

public sealed record IncidentOperationResult(IncidentProgressSnapshot Snapshot, IncidentRejection? Rejection)
{
    public bool IsAccepted => Rejection is null;
}
