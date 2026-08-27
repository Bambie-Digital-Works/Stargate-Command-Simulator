using System.Text.Json;
using WormholeWorlds.Application.Diagnostics;
using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Infrastructure.Diagnostics;

public sealed class DiagnosticReportWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public string Write(string directory, DiagnosticReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(directory);
        string fileName = $"diagnostics-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.json";
        string path = Path.Combine(directory, fileName);
        string json = JsonSerializer.Serialize(report, Options);
        AtomicJsonFileStore.WriteAtomically(
            path,
            json,
            candidate =>
            {
                using JsonDocument document = JsonDocument.Parse(candidate);
                return document.RootElement.TryGetProperty("build", out _)
                    && document.RootElement.TryGetProperty("recentEvents", out _);
            },
            backupSuffix: null);
        return path;
    }
}
