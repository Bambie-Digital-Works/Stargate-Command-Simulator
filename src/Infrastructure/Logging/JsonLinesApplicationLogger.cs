using System.Text.Json;
using System.Text.Json.Serialization;
using FacilityCommand.Application.Logging;

namespace FacilityCommand.Infrastructure.Logging;

public sealed class JsonLinesApplicationLogger : IApplicationLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly string _logDirectory;
    private readonly ApplicationLogLevel _minimumLevel;
    private readonly int _maxFiles;
    private readonly long _maxFileBytes;
    private readonly int _recentCapacity;
    private readonly SensitiveDataSanitizer _sanitizer;
    private readonly Queue<ApplicationLogEvent> _recent = new();

    public JsonLinesApplicationLogger(
        string logDirectory,
        ApplicationLogLevel minimumLevel,
        int maxFiles,
        long maxFileBytes,
        int recentCapacity,
        SensitiveDataSanitizer? sanitizer = null)
    {
        _logDirectory = logDirectory;
        _minimumLevel = minimumLevel;
        _maxFiles = maxFiles;
        _maxFileBytes = maxFileBytes;
        _recentCapacity = recentCapacity;
        _sanitizer = sanitizer ?? new SensitiveDataSanitizer();
        Directory.CreateDirectory(_logDirectory);
    }

    public IReadOnlyList<ApplicationLogEvent> Recent => _recent.ToArray();

    public void Log(
        ApplicationLogLevel level,
        string eventId,
        string message,
        IReadOnlyDictionary<string, string>? properties = null)
    {
        if (level < _minimumLevel)
        {
            return;
        }

        ApplicationLogEvent logEvent = new(
            DateTimeOffset.UtcNow,
            level,
            _sanitizer.Sanitize(eventId),
            _sanitizer.Sanitize(message),
            _sanitizer.SanitizeProperties(properties));

        _recent.Enqueue(logEvent);
        while (_recent.Count > _recentCapacity)
        {
            _recent.Dequeue();
        }

        string logPath = Path.Combine(_logDirectory, $"application-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        RotateIfRequired(logPath);
        File.AppendAllText(logPath, JsonSerializer.Serialize(logEvent, JsonOptions) + Environment.NewLine);
    }

    private void RotateIfRequired(string activePath)
    {
        if (!File.Exists(activePath) || new FileInfo(activePath).Length < _maxFileBytes)
        {
            return;
        }

        if (_maxFiles == 1)
        {
            File.Delete(activePath);
            return;
        }

        for (int index = _maxFiles - 1; index >= 1; index--)
        {
            string source = index == 1 ? activePath : $"{activePath}.{index - 1}";
            string destination = $"{activePath}.{index}";
            if (File.Exists(source))
            {
                File.Move(source, destination, true);
            }
        }
    }
}
