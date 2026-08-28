using System.Net.Http.Headers;
using System.Text.Json;
using WormholeWorlds.Application.Updates;

namespace WormholeWorlds.Infrastructure.Updates;

public sealed class GitHubReleaseFeedClient : IReleaseFeedClient
{
    private const string InstallerSuffix = "-Windows-x64-Setup.exe";
    private readonly HttpClient _httpClient;
    private readonly Uri _releasesUri;

    public GitHubReleaseFeedClient(HttpClient httpClient, string owner, string repository)
    {
        _httpClient = httpClient;
        _releasesUri = new Uri($"https://api.github.com/repos/{owner}/{repository}/releases?per_page=20");
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WormholeWorldsSimulator", "0.8"));
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(
        string currentVersion,
        ReleaseChannel channel,
        CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(currentVersion, out SemanticVersion? current))
        {
            return new UpdateCheckResult(UpdateCheckStatus.Failed, "The installed version is not valid SemVer.");
        }

        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(_releasesUri, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            GitHubRelease?[] releases = await JsonSerializer.DeserializeAsync<GitHubRelease?[]>(
                stream,
                cancellationToken: cancellationToken) ?? [];

            ReleaseDescriptor? newest = releases
                .Where(release => release is not null
                    && !release.Draft
                    && (channel == ReleaseChannel.Preview || !release.Prerelease))
                .Select(ToDescriptor)
                .Where(descriptor => descriptor is not null)
                .Cast<ReleaseDescriptor>()
                .Where(descriptor => SemanticVersion.TryParse(descriptor.Version, out SemanticVersion? candidate)
                    && candidate!.CompareTo(current) > 0)
                .OrderByDescending(descriptor => ParseRequired(descriptor.Version))
                .FirstOrDefault();

            return newest is null
                ? new UpdateCheckResult(UpdateCheckStatus.UpToDate, "Wormhole Worlds is up to date.")
                : new UpdateCheckResult(
                    UpdateCheckStatus.UpdateAvailable,
                    $"Version {newest.Version} is available.",
                    newest);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            return new UpdateCheckResult(
                UpdateCheckStatus.Failed,
                $"Update check unavailable: {exception.Message}");
        }
    }

    private static ReleaseDescriptor? ToDescriptor(GitHubRelease? release)
    {
        if (release is null || string.IsNullOrWhiteSpace(release.TagName) || release.Assets is null)
        {
            return null;
        }

        GitHubAsset? installer = release.Assets.FirstOrDefault(asset =>
            asset?.Name?.EndsWith(InstallerSuffix, StringComparison.OrdinalIgnoreCase) == true);
        GitHubAsset? checksums = release.Assets.FirstOrDefault(asset =>
            string.Equals(asset?.Name, "SHA256SUMS.txt", StringComparison.OrdinalIgnoreCase));
        if (installer?.Name is null
            || checksums is null
            || string.IsNullOrWhiteSpace(release.HtmlUrl)
            || string.IsNullOrWhiteSpace(installer.DownloadUrl)
            || string.IsNullOrWhiteSpace(checksums.DownloadUrl)
            || !Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out Uri? releasePage)
            || !Uri.TryCreate(installer.DownloadUrl, UriKind.Absolute, out Uri? installerUri)
            || !Uri.TryCreate(checksums.DownloadUrl, UriKind.Absolute, out Uri? checksumsUri))
        {
            return null;
        }

        return new ReleaseDescriptor(
            release.TagName.TrimStart('v', 'V'),
            release.Prerelease,
            release.Body ?? string.Empty,
            releasePage,
            installerUri,
            checksumsUri,
            installer.Size,
            installer.Name);
    }

    private static SemanticVersion ParseRequired(string value)
    {
        SemanticVersion.TryParse(value, out SemanticVersion? parsed);
        return parsed!;
    }

    private sealed record GitHubRelease(
        [property: System.Text.Json.Serialization.JsonPropertyName("tag_name")] string? TagName,
        [property: System.Text.Json.Serialization.JsonPropertyName("html_url")] string? HtmlUrl,
        [property: System.Text.Json.Serialization.JsonPropertyName("body")] string? Body,
        [property: System.Text.Json.Serialization.JsonPropertyName("draft")] bool Draft,
        [property: System.Text.Json.Serialization.JsonPropertyName("prerelease")] bool Prerelease,
        [property: System.Text.Json.Serialization.JsonPropertyName("assets")] GitHubAsset?[]? Assets);

    private sealed record GitHubAsset(
        [property: System.Text.Json.Serialization.JsonPropertyName("name")] string? Name,
        [property: System.Text.Json.Serialization.JsonPropertyName("browser_download_url")] string? DownloadUrl,
        [property: System.Text.Json.Serialization.JsonPropertyName("size")] long Size);
}
