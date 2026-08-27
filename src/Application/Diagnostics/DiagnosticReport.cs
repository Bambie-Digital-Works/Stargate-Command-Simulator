using WormholeWorlds.Application.Logging;

namespace WormholeWorlds.Application.Diagnostics;

public sealed record DiagnosticReport(
    BuildMetadata Build,
    IReadOnlyList<DiagnosticReportEvent> RecentEvents)
{
    public static DiagnosticReport Create(
        BuildMetadata metadata,
        IReadOnlyList<ApplicationLogEvent> events)
    {
        return new DiagnosticReport(
            metadata,
            events.Select(item => new DiagnosticReportEvent(
                item.TimestampUtc,
                item.Level.ToString(),
                item.EventId,
                item.Message,
                item.Properties)).ToArray());
    }
}
