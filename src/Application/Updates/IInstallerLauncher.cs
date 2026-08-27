namespace WormholeWorlds.Application.Updates;

public interface IInstallerLauncher
{
    bool TryLaunch(string installerPath, out string? error);
}
