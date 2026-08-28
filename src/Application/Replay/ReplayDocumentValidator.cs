using System.Text.RegularExpressions;

namespace WormholeWorlds.Application.Replay;

public static partial class ReplayDocumentValidator
{
    public static bool IsValid(ReplayDocument? document)
    {
        if (document is null
            || document.SchemaVersion != 1
            || document.ContentSchemaVersion < 1
            || document.Records is null
            || !Sha256Pattern().IsMatch(document.Sha256))
        {
            return false;
        }

        long previousMilliseconds = -1;
        for (int index = 0; index < document.Records.Count; index++)
        {
            ReplayRecord? record = document.Records[index];
            if (record is null
                || record.Sequence != index + 1L
                || record.SimulationMilliseconds < previousMilliseconds
                || !StableIdPattern().IsMatch(record.Kind)
                || !StableIdPattern().IsMatch(record.Command)
                || !StableIdPattern().IsMatch(record.Outcome)
                || !StableIdPattern().IsMatch(record.ReasonCode))
            {
                return false;
            }

            previousMilliseconds = record.SimulationMilliseconds;
        }

        return true;
    }

    [GeneratedRegex("^[a-z0-9_]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex StableIdPattern();

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
