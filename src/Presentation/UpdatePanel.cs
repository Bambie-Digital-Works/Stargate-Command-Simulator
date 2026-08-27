using Godot;
using WormholeWorlds.Application.Updates;
using WormholeWorlds.Infrastructure.Updates;

namespace WormholeWorlds.Presentation;

public partial class UpdatePanel : PanelContainer
{
    private Label _status = null!;
    private Label _notes = null!;
    private Button _checkButton = null!;
    private Button _installButton = null!;
    private UpdateCheckCoordinator? _coordinator;
    private IInstallerDownloader? _downloader;
    private IInstallerLauncher? _launcher;
    private string _currentVersion = string.Empty;
    private string _downloadDirectory = string.Empty;
    private ReleaseDescriptor? _availableRelease;

    public event Action? CloseRequested;
    public event Action? UpdateAvailable;

    public override void _Ready()
    {
        _status = GetNode<Label>("Margin/Layout/Status");
        _notes = GetNode<Label>("Margin/Layout/NotesScroll/Notes");
        _checkButton = GetNode<Button>("Margin/Layout/Actions/CheckButton");
        _installButton = GetNode<Button>("Margin/Layout/Actions/InstallButton");
        _checkButton.Pressed += () => _ = CheckAsync(manual: true);
        _installButton.Pressed += () => _ = DownloadAndInstallAsync();
        GetNode<Button>("Margin/Layout/Actions/LaterButton").Pressed += () => CloseRequested?.Invoke();
    }

    public void Initialize(
        UpdateCheckCoordinator coordinator,
        IInstallerDownloader downloader,
        IInstallerLauncher launcher,
        string currentVersion,
        string downloadDirectory)
    {
        _coordinator = coordinator;
        _downloader = downloader;
        _launcher = launcher;
        _currentVersion = currentVersion;
        _downloadDirectory = downloadDirectory;
        _status.Text = $"Installed version: {currentVersion}";
        _notes.Text = "Use Check now to query the public preview release feed.";
        _installButton.Disabled = true;
        _ = CheckAsync(manual: false);
    }

    public void FocusPrimaryAction() => (_installButton.Disabled ? _checkButton : _installButton).GrabFocus();

    public async Task CheckAsync(bool manual)
    {
        if (_coordinator is null)
        {
            return;
        }

        _checkButton.Disabled = true;
        _status.Text = "Checking for preview updates…";
        UpdateCheckResult result = await _coordinator.CheckAsync(
            _currentVersion,
            ReleaseChannel.Preview,
            manual);
        _checkButton.Disabled = false;
        _status.Text = result.Message;
        _availableRelease = result.Release;
        _installButton.Disabled = !result.IsUpdateAvailable;

        if (result.Release is { } release)
        {
            _notes.Text =
                $"Version {release.Version} — {FormatSize(release.InstallerSizeBytes)}\n\n{release.ReleaseNotes}";
            UpdateAvailable?.Invoke();
        }
        else if (manual)
        {
            _notes.Text = result.Status == UpdateCheckStatus.Failed
                ? "The game remains fully playable offline. Try again later or use the release page."
                : "No newer compatible Windows x64 preview is available.";
        }
    }

    private async Task DownloadAndInstallAsync()
    {
        if (_availableRelease is null || _downloader is null || _launcher is null)
        {
            return;
        }

        _checkButton.Disabled = true;
        _installButton.Disabled = true;
        _status.Text = "Downloading and verifying the installer…";
        try
        {
            string path = await _downloader.DownloadAndVerifyAsync(_availableRelease, _downloadDirectory);
            if (_launcher.TryLaunch(path, out string? error))
            {
                _status.Text = "Verified installer launched. Closing the game for update.";
                GetTree().Quit();
                return;
            }

            _status.Text = $"Could not launch the installer: {error}";
        }
        catch (Exception exception) when (exception is
            HttpRequestException or
            IOException or
            UnauthorizedAccessException or
            InvalidDataException or
            TaskCanceledException)
        {
            _status.Text = $"Update was not installed: {exception.Message}";
        }
        finally
        {
            _checkButton.Disabled = false;
            _installButton.Disabled = _availableRelease is null;
        }
    }

    private static string FormatSize(long bytes) => bytes <= 0
        ? "size unavailable"
        : $"{bytes / 1024d / 1024d:0.0} MB";
}
