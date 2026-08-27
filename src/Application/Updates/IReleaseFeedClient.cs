namespace WormholeWorlds.Application.Updates;

public interface IReleaseFeedClient
{
    Task<UpdateCheckResult> CheckAsync(
        string currentVersion,
        ReleaseChannel channel,
        CancellationToken cancellationToken = default);
}
