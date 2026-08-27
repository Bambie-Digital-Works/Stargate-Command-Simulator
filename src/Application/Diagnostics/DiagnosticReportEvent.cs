namespace WormholeWorlds.Application.Diagnostics;

public sealed record DiagnosticReportEvent(
    DateTimeOffset TimestampUtc,
    string Level,
    string EventId,
    string Message,
    IReadOnlyDictionary<string, string> Properties);
