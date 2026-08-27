namespace WormholeWorlds.Application.Updates;

public interface IInstallerDownloader
{
    Task<string> DownloadAndVerifyAsync(
        ReleaseDescriptor release,
        string destinationDirectory,
        CancellationToken cancellationToken = default);
}
