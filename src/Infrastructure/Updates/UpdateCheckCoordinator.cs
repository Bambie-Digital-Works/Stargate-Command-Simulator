using System.Text.Json;
using WormholeWorlds.Application.Updates;
using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Infrastructure.Updates;

public sealed class UpdateCheckCoordinator
{
    private static readonly TimeSpan AutomaticCheckInterval = TimeSpan.FromHours(24);
    private readonly IReleaseFeedClient _client;
    private readonly string _cachePath;
    private readonly Func<DateTimeOffset> _utcNow;

    public UpdateCheckCoordinator(
        IReleaseFeedClient client,
        string cachePath,
        Func<DateTimeOffset>? utcNow = null)
    {
        _client = client;
        _cachePath = cachePath;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<UpdateCheckResult> CheckAsync(
        string currentVersion,
        ReleaseChannel channel,
        bool manual,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = _utcNow();
        if (!manual && TryReadLastCheck(out DateTimeOffset lastCheck)
            && lastCheck <= now
            && now - lastCheck < AutomaticCheckInterval)
        {
            return new UpdateCheckResult(UpdateCheckStatus.NotDue, "Automatic update check is not due yet.");
        }

        UpdateCheckResult result = await _client.CheckAsync(currentVersion, channel, cancellationToken);
        if (result.Status is UpdateCheckStatus.UpToDate or UpdateCheckStatus.UpdateAvailable)
        {
            TrySaveLastCheck(_utcNow());
        }

        return result;
    }

    private bool TryReadLastCheck(out DateTimeOffset lastCheck)
    {
        lastCheck = default;
        try
        {
            if (!File.Exists(_cachePath))
            {
                return false;
            }

            UpdateCheckCache? cache = JsonSerializer.Deserialize<UpdateCheckCache>(File.ReadAllText(_cachePath));
            lastCheck = cache?.LastSuccessfulCheckUtc ?? default;
            return cache?.SchemaVersion == 1 && lastCheck != default;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return false;
        }
    }

    private void TrySaveLastCheck(DateTimeOffset timestamp)
    {
        try
        {
            string json = JsonSerializer.Serialize(new UpdateCheckCache(1, timestamp));
            AtomicJsonFileStore.WriteAtomically(
                _cachePath,
                json,
                candidate => JsonSerializer.Deserialize<UpdateCheckCache>(candidate)?.SchemaVersion == 1,
                backupSuffix: null);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Cache persistence is best effort; a successful update check remains successful.
        }
    }

    private sealed record UpdateCheckCache(int SchemaVersion, DateTimeOffset LastSuccessfulCheckUtc);
}
