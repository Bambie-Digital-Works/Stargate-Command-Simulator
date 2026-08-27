using System.Text.Json;
using WormholeWorlds.Application.Diagnostics;
using WormholeWorlds.Application.Logging;
using WormholeWorlds.Infrastructure.Diagnostics;
using WormholeWorlds.Infrastructure.Logging;

namespace WormholeWorlds.Tests;

public sealed class DiagnosticsAndLoggingTests
{
    [Fact]
    public void ReadsCompleteBuildMetadataWithoutGodotRuntime()
    {
        RuntimeBuildContext runtime = new("4.7.2.stable.mono", ".NET 8.0", "Test OS", "x64");

        var metadata = new AssemblyBuildMetadataProvider(typeof(AssemblyBuildMetadataProvider).Assembly, runtime).Get();

        string expectedChannel = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BuildChannel"))
            ? "development"
            : Environment.GetEnvironmentVariable("BuildChannel")!;
        string expectedBuildId = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("BuildId"))
            ? "local"
            : Environment.GetEnvironmentVariable("BuildId")!;
        string expectedCommit = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CommitSha"))
            ? "unknown"
            : Environment.GetEnvironmentVariable("CommitSha")!;
        Assert.Equal(expectedChannel, metadata.Channel);
        Assert.Equal(expectedBuildId, metadata.BuildId);
        Assert.Equal(expectedCommit, metadata.CommitSha);
        Assert.Equal(1, metadata.ContentSchemaVersion);
        Assert.Equal(2, metadata.SaveSchemaVersion);
        Assert.Equal(TimeSpan.Zero, metadata.BuildUtc.Offset);
        Assert.Equal("4.7.2.stable.mono", metadata.EngineVersion);
        Assert.Equal("x64", metadata.Architecture);
    }

    [Fact]
    public void SanitizerRemovesPathsCredentialsAndUnexpectedProperties()
    {
        SensitiveDataSanitizer sanitizer = new("Alice");
        string sanitized = sanitizer.Sanitize(@"token=abc123 failed at C:\Users\Alice\save.json and /home/alice/config");
        IReadOnlyDictionary<string, string> properties = sanitizer.SanitizeProperties(
            new Dictionary<string, string>
            {
                ["component"] = @"C:\Users\Alice\component",
                ["rawPayload"] = "private data",
            });

        Assert.DoesNotContain("abc123", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("Alice", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/home/alice", sanitized, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("<path>", properties["component"]);
        Assert.Equal("<redacted>", properties["rawPayload"]);
    }

    [Fact]
    public void LoggerWritesJsonLinesAndRotates()
    {
        using TestDirectory directory = new();
        JsonLinesApplicationLogger logger = new(
            directory.Path,
            ApplicationLogLevel.Debug,
            maxFiles: 2,
            maxFileBytes: 65536,
            recentCapacity: 2,
            new SensitiveDataSanitizer("Alice"));

        logger.Log(ApplicationLogLevel.Information, "test.started", "Started for Alice.");
        logger.Log(ApplicationLogLevel.Warning, "test.warning", "password=secret");
        logger.Log(ApplicationLogLevel.Error, "test.large", new string('x', 66000));
        logger.Log(ApplicationLogLevel.Error, "test.failed", "Failure recorded.");

        string logPath = Directory.GetFiles(directory.Path, "*.jsonl").Single();
        string[] lines = File.ReadAllLines(logPath);
        Assert.Single(lines);
        Assert.True(File.Exists(logPath + ".1"));
        Assert.Equal(2, logger.Recent.Count);
        string allLogText = string.Join('\n', Directory.GetFiles(directory.Path).SelectMany(File.ReadAllLines));
        Assert.DoesNotContain("Alice", allLogText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", allLogText, StringComparison.OrdinalIgnoreCase);
        foreach (string line in Directory.GetFiles(directory.Path).SelectMany(File.ReadAllLines))
        {
            using JsonDocument _ = JsonDocument.Parse(line);
        }
    }

    [Fact]
    public void DiagnosticReportExportContainsOnlyBuildIdentityAndSanitizedRecentEvents()
    {
        using TestDirectory directory = new();
        var metadata = new BuildMetadata(
            "0.8.0-beta.1",
            "preview",
            "42",
            "abc123",
            1,
            2,
            DateTimeOffset.UnixEpoch,
            "4.7.2.stable.mono",
            ".NET 8.0",
            "Windows",
            "x64");
        var logger = new JsonLinesApplicationLogger(
            directory.Path,
            ApplicationLogLevel.Debug,
            maxFiles: 2,
            maxFileBytes: 65536,
            recentCapacity: 10,
            new SensitiveDataSanitizer("Alice"));
        logger.Log(
            ApplicationLogLevel.Warning,
            "diagnostic.warning",
            @"password=secret at C:\Users\Alice\save.json",
            new Dictionary<string, string>
            {
                ["component"] = "save",
                ["rawPayload"] = "save contents",
            });

        DiagnosticReport report = DiagnosticReport.Create(metadata, logger.Recent);
        string path = new DiagnosticReportWriter().Write(directory.Path, report);
        string json = File.ReadAllText(path);

        Assert.Contains("0.8.0-beta.1", json, StringComparison.Ordinal);
        Assert.Contains("diagnostic.warning", json, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alice", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("save contents", json, StringComparison.OrdinalIgnoreCase);
        using JsonDocument _ = JsonDocument.Parse(json);
    }
}
