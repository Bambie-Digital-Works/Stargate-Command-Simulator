namespace FacilityCommand.Application.Logging;

public sealed record ApplicationLogEvent(
    DateTimeOffset TimestampUtc,
    ApplicationLogLevel Level,
    string EventId,
    string Message,
    IReadOnlyDictionary<string, string> Properties);

