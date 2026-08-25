namespace FacilityCommand.Application.Logging;

public interface IApplicationLogger
{
    IReadOnlyList<ApplicationLogEvent> Recent { get; }

    void Log(
        ApplicationLogLevel level,
        string eventId,
        string message,
        IReadOnlyDictionary<string, string>? properties = null);
}

