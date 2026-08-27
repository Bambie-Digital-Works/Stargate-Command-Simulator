using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Application.Replay;

public sealed partial class ReplayRecorder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly List<ReplayRecord> _records = [];
    private readonly int _contentSchemaVersion;
    private readonly ulong _seed;

    public ReplayRecorder(int contentSchemaVersion, ulong seed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(contentSchemaVersion);
        _contentSchemaVersion = contentSchemaVersion;
        _seed = seed;
    }

    public IReadOnlyList<ReplayRecord> Records => _records;

    public ReplayRecord Record(TransitArrayCommand command, TransitTransitionResult result)
    {
        long sequence = _records.Count + 1L;
        ReplayRecord record = new(
            sequence,
            command.At.Milliseconds,
            result.IsAccepted ? "event" : "rejection",
            ToStableId(command.Kind.ToString()),
            result.IsAccepted ? ToStableId(result.Snapshot.Phase.ToString()) : "rejected",
            SafeReason(result.Event?.ReasonCode ?? result.Rejection?.ReasonCode ?? "unknown"));
        _records.Add(record);
        return record;
    }

    public ReplayDocument Build()
    {
        ReplayPayload payload = new(1, _contentSchemaVersion, _seed, _records.ToArray());
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        string checksum = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
        return new ReplayDocument(payload.SchemaVersion, payload.ContentSchemaVersion, payload.Seed, payload.Records, checksum);
    }

    public static bool Verify(ReplayDocument document)
    {
        if (document.Sha256.Length != 64)
        {
            return false;
        }

        ReplayPayload payload = new(document.SchemaVersion, document.ContentSchemaVersion, document.Seed, document.Records);
        byte[] canonical = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        string checksum = Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(checksum),
            Encoding.ASCII.GetBytes(document.Sha256));
    }

    public string Serialize()
    {
        ReplayDocument document = Build();
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    private static string SafeReason(string value) => StableIdPattern().IsMatch(value) ? value : "redacted";

    private static string ToStableId(string value)
    {
        StringBuilder result = new();
        foreach (char character in value)
        {
            if (char.IsUpper(character) && result.Length > 0)
            {
                result.Append('_');
            }

            result.Append(char.ToLowerInvariant(character));
        }

        return result.ToString();
    }

    [GeneratedRegex("^[a-z0-9_]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();

    private sealed record ReplayPayload(
        int SchemaVersion,
        int ContentSchemaVersion,
        ulong Seed,
        IReadOnlyList<ReplayRecord> Records);
}
