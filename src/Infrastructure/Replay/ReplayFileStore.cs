using System.Text;
using System.Text.Json;
using WormholeWorlds.Application.Replay;
using WormholeWorlds.Infrastructure.Persistence;

namespace WormholeWorlds.Infrastructure.Replay;

public sealed class ReplayFileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly string _directory;

    public ReplayFileStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    public string Save(string replayId, string json)
    {
        ValidateReplayId(replayId);

        ReplayDocument document;
        try
        {
            document = JsonSerializer.Deserialize<ReplayDocument>(json, JsonOptions)
                ?? throw new InvalidDataException("Replay document is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Replay document contains invalid JSON.", exception);
        }

        if (!ReplayRecorder.Verify(document))
        {
            throw new InvalidDataException("Replay document failed schema or checksum validation.");
        }

        Directory.CreateDirectory(_directory);
        string target = Path.Combine(_directory, $"{replayId}.replay.json");
        string canonical = JsonSerializer.Serialize(document, JsonOptions);
        AtomicJsonFileStore.WriteAtomically(
            target,
            canonical,
            candidate => TryDeserializeAndVerify(candidate));
        return target;
    }

    public ReplayDocument Load(string replayId)
    {
        ValidateReplayId(replayId);
        string path = Path.Combine(_directory, $"{replayId}.replay.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Replay document was not found.", path);
        }

        try
        {
            ReplayDocument document = JsonSerializer.Deserialize<ReplayDocument>(
                File.ReadAllText(path),
                JsonOptions)
                ?? throw new InvalidDataException("Replay document is empty.");
            if (!ReplayRecorder.Verify(document))
            {
                throw new InvalidDataException("Replay document failed schema or checksum validation.");
            }

            return document;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Replay document contains invalid JSON.", exception);
        }
    }

    private static bool TryDeserializeAndVerify(string json)
    {
        try
        {
            ReplayDocument? document = JsonSerializer.Deserialize<ReplayDocument>(json, JsonOptions);
            return ReplayRecorder.Verify(document);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidateReplayId(string replayId)
    {
        ArgumentNullException.ThrowIfNull(replayId);
        if (replayId.Length is < 1 or > 64
            || replayId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            throw new ArgumentException("Replay ID contains unsupported characters.", nameof(replayId));
        }
    }
}
