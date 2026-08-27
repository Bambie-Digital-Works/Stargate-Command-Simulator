namespace WormholeWorlds.Application.Updates;

public sealed class SemanticVersion : IComparable<SemanticVersion>
{
    private SemanticVersion(int major, int minor, int patch, IReadOnlyList<string> prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }
    public IReadOnlyList<string> Prerelease { get; }

    public static bool TryParse(string? value, out SemanticVersion? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        string[] buildParts = normalized.Split('+');
        if (buildParts.Length > 2
            || buildParts.Length == 2 && !AreValidIdentifiers(buildParts[1].Split('.'), rejectNumericLeadingZero: false))
        {
            return false;
        }

        normalized = buildParts[0];
        string[] releaseParts = normalized.Split('-', 2);
        string[] core = releaseParts[0].Split('.');
        if (core.Length != 3
            || core.Any(HasNumericLeadingZero)
            || !int.TryParse(core[0], out int major)
            || !int.TryParse(core[1], out int minor)
            || !int.TryParse(core[2], out int patch)
            || major < 0
            || minor < 0
            || patch < 0)
        {
            return false;
        }

        string[] prerelease = releaseParts.Length == 2 ? releaseParts[1].Split('.') : [];
        if (!AreValidIdentifiers(prerelease, rejectNumericLeadingZero: true))
        {
            return false;
        }

        version = new SemanticVersion(major, minor, patch, prerelease);
        return true;
    }

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        int core = Major.CompareTo(other.Major);
        if (core == 0) core = Minor.CompareTo(other.Minor);
        if (core == 0) core = Patch.CompareTo(other.Patch);
        if (core != 0) return core;

        if (Prerelease.Count == 0 && other.Prerelease.Count == 0) return 0;
        if (Prerelease.Count == 0) return 1;
        if (other.Prerelease.Count == 0) return -1;

        for (int index = 0; index < Math.Max(Prerelease.Count, other.Prerelease.Count); index++)
        {
            if (index >= Prerelease.Count) return -1;
            if (index >= other.Prerelease.Count) return 1;
            string left = Prerelease[index];
            string right = other.Prerelease[index];
            bool leftNumeric = left.All(char.IsAsciiDigit);
            bool rightNumeric = right.All(char.IsAsciiDigit);
            int comparison = (leftNumeric, rightNumeric) switch
            {
                (true, true) => CompareNumericIdentifiers(left, right),
                (true, false) => -1,
                (false, true) => 1,
                _ => string.CompareOrdinal(left, right),
            };
            if (comparison != 0) return comparison;
        }

        return 0;
    }

    private static bool AreValidIdentifiers(IEnumerable<string> identifiers, bool rejectNumericLeadingZero) =>
        identifiers.All(identifier =>
            !string.IsNullOrWhiteSpace(identifier)
            && identifier.All(character => char.IsAsciiLetterOrDigit(character) || character == '-')
            && (!rejectNumericLeadingZero || !HasNumericLeadingZero(identifier)));

    private static bool HasNumericLeadingZero(string identifier) =>
        identifier.Length > 1 && identifier[0] == '0' && identifier.All(char.IsAsciiDigit);

    private static int CompareNumericIdentifiers(string left, string right)
    {
        int length = left.Length.CompareTo(right.Length);
        return length != 0 ? length : string.CompareOrdinal(left, right);
    }
}
