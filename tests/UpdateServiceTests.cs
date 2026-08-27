using System.Net;
using System.Security.Cryptography;
using System.Text;
using WormholeWorlds.Application.Updates;
using WormholeWorlds.Infrastructure.Updates;

namespace WormholeWorlds.Tests;

public sealed class UpdateServiceTests
{
    [Theory]
    [InlineData("0.8.0-beta.1", "0.8.0-beta.2", -1)]
    [InlineData("0.8.0-beta.2", "0.8.0", -1)]
    [InlineData("0.8.0", "0.8.0-beta.9", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    public void SemanticVersionUsesPrereleasePrecedence(string left, string right, int expectedSign)
    {
        Assert.True(SemanticVersion.TryParse(left, out SemanticVersion? leftVersion));
        Assert.True(SemanticVersion.TryParse(right, out SemanticVersion? rightVersion));
        Assert.Equal(expectedSign, Math.Sign(leftVersion!.CompareTo(rightVersion)));
    }

    [Theory]
    [InlineData("vv1.0.0")]
    [InlineData("01.0.0")]
    [InlineData("1.0.0-beta.01")]
    [InlineData("1.0.0+bad metadata")]
    [InlineData("1.0.0+")]
    public void SemanticVersionRejectsMalformedValues(string value)
    {
        Assert.False(SemanticVersion.TryParse(value, out _));
    }

    [Fact]
    public async Task PreviewFeedSelectsNewerPrereleaseWithRequiredAssets()
    {
        string json = """
            [
              {
                "tag_name": "v0.8.0-beta.2",
                "html_url": "https://example.test/release",
                "body": "Fixes",
                "draft": false,
                "prerelease": true,
                "assets": [
                  {"name":"Wormhole-Worlds-Simulator-0.8.0-beta.2-Windows-x64-Setup.exe","browser_download_url":"https://example.test/setup.exe","size":42},
                  {"name":"SHA256SUMS.txt","browser_download_url":"https://example.test/SHA256SUMS.txt","size":90}
                ]
              }
            ]
            """;
        HttpClient httpClient = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        }));
        GitHubReleaseFeedClient client = new(httpClient, "owner", "repo");

        UpdateCheckResult result = await client.CheckAsync("0.8.0-beta.1", ReleaseChannel.Preview);

        Assert.True(result.IsUpdateAvailable);
        Assert.Equal("0.8.0-beta.2", result.Release!.Version);
    }

    [Fact]
    public async Task DownloaderRejectsTamperedInstaller()
    {
        byte[] installer = Encoding.UTF8.GetBytes("tampered");
        string manifest = $"{new string('0', 64)}  setup.exe";
        HttpClient client = new(new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = request.RequestUri!.AbsolutePath.EndsWith("sums", StringComparison.Ordinal)
                ? new StringContent(manifest)
                : new ByteArrayContent(installer),
        }));
        ReleaseDescriptor release = new(
            "0.8.0-beta.2",
            true,
            string.Empty,
            new Uri("https://example.test/release"),
            new Uri("https://example.test/setup.exe"),
            new Uri("https://example.test/sums"),
            installer.Length,
            "setup.exe");
        using TestDirectory directory = new();

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new VerifiedInstallerDownloader(client).DownloadAndVerifyAsync(release, directory.Path));
    }

    [Fact]
    public async Task DownloaderRejectsUnsafeInstallerFilenameBeforeWriting()
    {
        HttpClient client = new(new StubHandler(_ => throw new InvalidOperationException("HTTP should not be called.")));
        ReleaseDescriptor release = CreateRelease("../setup.exe", installerSizeBytes: 1);
        using TestDirectory directory = new();

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new VerifiedInstallerDownloader(client).DownloadAndVerifyAsync(release, directory.Path));
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task DownloaderRejectsUnexpectedInstallerSizeAndCleansTemporaryFile()
    {
        byte[] installer = Encoding.UTF8.GetBytes("installer");
        string hash = Convert.ToHexString(SHA256.HashData(installer)).ToLowerInvariant();
        string manifest = $"{hash}  setup.exe";
        HttpClient client = new(new StubHandler(request => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = request.RequestUri!.AbsolutePath.EndsWith("sums", StringComparison.Ordinal)
                ? new StringContent(manifest)
                : new ByteArrayContent(installer),
        }));
        ReleaseDescriptor release = CreateRelease("setup.exe", installer.Length + 1);
        using TestDirectory directory = new();

        await Assert.ThrowsAsync<InvalidDataException>(() =>
            new VerifiedInstallerDownloader(client).DownloadAndVerifyAsync(release, directory.Path));
        Assert.Empty(Directory.GetFiles(directory.Path));
    }

    [Fact]
    public async Task AutomaticChecksAreLimitedToOncePerDay()
    {
        using TestDirectory directory = new();
        DateTimeOffset now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
        StubReleaseFeed feed = new();
        UpdateCheckCoordinator coordinator = new(
            feed,
            Path.Combine(directory.Path, "cache.json"),
            () => now);

        await coordinator.CheckAsync("0.8.0-beta.1", ReleaseChannel.Preview, manual: false);
        UpdateCheckResult second = await coordinator.CheckAsync(
            "0.8.0-beta.1",
            ReleaseChannel.Preview,
            manual: false);

        Assert.Equal(1, feed.CallCount);
        Assert.Equal(UpdateCheckStatus.NotDue, second.Status);
    }

    [Fact]
    public async Task FutureCacheTimestampDoesNotSuppressAutomaticCheck()
    {
        using TestDirectory directory = new();
        DateTimeOffset now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
        string cachePath = Path.Combine(directory.Path, "cache.json");
        File.WriteAllText(
            cachePath,
            "{\"SchemaVersion\":1,\"LastSuccessfulCheckUtc\":\"2026-08-28T12:00:00+00:00\"}");
        StubReleaseFeed feed = new();
        UpdateCheckCoordinator coordinator = new(feed, cachePath, () => now);

        UpdateCheckResult result = await coordinator.CheckAsync(
            "0.8.0-beta.1",
            ReleaseChannel.Preview,
            manual: false);

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
        Assert.Equal(1, feed.CallCount);
    }

    [Fact]
    public async Task CacheWriteFailureDoesNotReplaceSuccessfulCheckResult()
    {
        using TestDirectory directory = new();
        string blockedParent = Path.Combine(directory.Path, "cache-parent-is-a-file");
        File.WriteAllText(blockedParent, "blocking file");
        UpdateCheckCoordinator coordinator = new(
            new StubReleaseFeed(),
            Path.Combine(blockedParent, "cache.json"));

        UpdateCheckResult result = await coordinator.CheckAsync(
            "0.8.0-beta.1",
            ReleaseChannel.Preview,
            manual: true);

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
    }

    [Fact]
    public async Task MalformedReleaseEntryIsIgnoredWithoutFailingCheck()
    {
        HttpClient httpClient = new(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"tag_name\":\"v0.8.0-beta.2\",\"assets\":null}]"),
        }));

        UpdateCheckResult result = await new GitHubReleaseFeedClient(httpClient, "owner", "repo")
            .CheckAsync("0.8.0-beta.1", ReleaseChannel.Preview);

        Assert.Equal(UpdateCheckStatus.UpToDate, result.Status);
    }

    private static ReleaseDescriptor CreateRelease(string fileName, long installerSizeBytes) => new(
        "0.8.0-beta.2",
        true,
        string.Empty,
        new Uri("https://example.test/release"),
        new Uri("https://example.test/setup.exe"),
        new Uri("https://example.test/sums"),
        installerSizeBytes,
        fileName);

    private sealed class StubReleaseFeed : IReleaseFeedClient
    {
        public int CallCount { get; private set; }

        public Task<UpdateCheckResult> CheckAsync(
            string currentVersion,
            ReleaseChannel channel,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new UpdateCheckResult(UpdateCheckStatus.UpToDate, "Current"));
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> callback) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(callback(request));
    }
}
