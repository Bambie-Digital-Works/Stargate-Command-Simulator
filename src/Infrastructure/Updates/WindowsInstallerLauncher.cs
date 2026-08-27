using System.Diagnostics;
using WormholeWorlds.Application.Updates;

namespace WormholeWorlds.Infrastructure.Updates;

public sealed class WindowsInstallerLauncher : IInstallerLauncher
{
    public bool TryLaunch(string installerPath, out string? error)
    {
        error = null;
        if (!OperatingSystem.IsWindows() || !File.Exists(installerPath))
        {
            error = "The verified Windows installer could not be found.";
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = installerPath,
                Arguments = "/SP- /CURRENTUSER",
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            error = exception.Message;
            return false;
        }
    }
}
