namespace WormholeWorlds.Application.Updates;

public enum UpdateCheckStatus
{
    NotDue,
    UpToDate,
    UpdateAvailable,
    Failed,
}

public sealed record UpdateCheckResult(
    UpdateCheckStatus Status,
    string Message,
    ReleaseDescriptor? Release = null)
{
    public bool IsUpdateAvailable => Status == UpdateCheckStatus.UpdateAvailable && Release is not null;
}
