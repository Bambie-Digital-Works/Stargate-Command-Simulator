using System.Text;

namespace FacilityCommand.Infrastructure.Replay;

public sealed class ReplayFileStore
{
    private readonly string _directory;

    public ReplayFileStore(string directory)
    {
        _directory = Path.GetFullPath(directory);
    }

    public string Save(string replayId, string json)
    {
        if (replayId.Length is < 1 or > 64 || replayId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '-'))
        {
            throw new ArgumentException("Replay ID contains unsupported characters.", nameof(replayId));
        }

        Directory.CreateDirectory(_directory);
        string target = Path.Combine(_directory, $"{replayId}.replay.json");
        string temporary = target + ".tmp";
        File.WriteAllText(temporary, json, new UTF8Encoding(false));
        File.Move(temporary, target, true);
        return target;
    }
}
