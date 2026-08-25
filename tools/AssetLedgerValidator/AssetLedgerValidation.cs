using System.Globalization;
using System.Text.Json;

namespace FacilityCommand.Tools.AssetLedgerValidator;

public static class AssetLedgerValidation
{
    private static readonly HashSet<string> AssetExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".svg",
        ".wav", ".ogg", ".mp3", ".flac",
        ".ttf", ".otf", ".glb", ".gltf", ".blend", ".mp4", ".webm",
    };

    private static readonly HashSet<string> Categories = new(StringComparer.Ordinal)
    {
        "image", "audio", "font", "video", "model", "reference-image", "reference-audio",
    };

    private static readonly HashSet<string> Origins = new(StringComparer.Ordinal)
    {
        "original", "commissioned", "licensed-third-party", "public-domain", "generated",
    };

    private static readonly HashSet<string> Scopes = new(StringComparer.Ordinal)
    {
        "production", "reference",
    };

    private static readonly HashSet<string> ReviewStatuses = new(StringComparer.Ordinal)
    {
        "unverified", "approved", "reference-only", "rejected",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
    };

    public static IReadOnlyList<string> Validate(string repositoryRoot, string ledgerRelativePath = "assets/asset-ledger.json")
    {
        List<string> errors = [];
        string root = Path.GetFullPath(repositoryRoot);
        string ledgerPath = ResolveInsideRoot(root, ledgerRelativePath, errors, "ledger");
        if (errors.Count > 0 || !File.Exists(ledgerPath))
        {
            errors.Add($"Ledger file not found: {ledgerRelativePath}");
            return errors;
        }

        AssetLedger? ledger;
        try
        {
            ledger = JsonSerializer.Deserialize<AssetLedger>(File.ReadAllText(ledgerPath), JsonOptions);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            errors.Add($"Ledger could not be read: {exception.Message}");
            return errors;
        }

        if (ledger is null)
        {
            errors.Add("Ledger is empty.");
            return errors;
        }

        if (ledger.SchemaVersion != 1)
        {
            errors.Add($"Unsupported ledger schemaVersion '{ledger.SchemaVersion}'; expected 1.");
        }

        IReadOnlyList<AssetRecord> assets = ledger.Assets ?? [];
        HashSet<string> ids = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < assets.Count; index++)
        {
            ValidateRecord(root, assets[index], index, ids, paths, errors);
        }

        foreach (string discoveredPath in DiscoverRetainedAssets(root))
        {
            if (!paths.Contains(discoveredPath))
            {
                errors.Add($"Retained asset has no ledger entry: {discoveredPath}");
            }
        }

        return errors;
    }

    private static void ValidateRecord(
        string root,
        AssetRecord asset,
        int index,
        HashSet<string> ids,
        HashSet<string> paths,
        List<string> errors)
    {
        string prefix = $"assets[{index}]";
        Require(asset.Id, "id", prefix, errors);
        Require(asset.Path, "path", prefix, errors);
        Require(asset.Category, "category", prefix, errors);
        Require(asset.Scope, "scope", prefix, errors);
        Require(asset.Origin, "origin", prefix, errors);
        Require(asset.Creator, "creator", prefix, errors);
        Require(asset.SourceUrl, "sourceUrl", prefix, errors);
        Require(asset.LicenseExpression, "licenseExpression", prefix, errors);
        Require(asset.AcquiredOn, "acquiredOn", prefix, errors);
        Require(asset.Modifications, "modifications", prefix, errors);
        Require(asset.Attribution, "attribution", prefix, errors);
        Require(asset.ProofLocation, "proofLocation", prefix, errors);
        Require(asset.ReviewStatus, "reviewStatus", prefix, errors);
        Require(asset.ReviewedBy, "reviewedBy", prefix, errors);
        Require(asset.ReviewedOn, "reviewedOn", prefix, errors);

        if (!string.IsNullOrWhiteSpace(asset.Id) && !ids.Add(asset.Id))
        {
            errors.Add($"{prefix}.id is duplicated: {asset.Id}");
        }

        string? normalizedPath = NormalizeRelativePath(asset.Path);
        if (normalizedPath is not null)
        {
            if (!paths.Add(normalizedPath))
            {
                errors.Add($"{prefix}.path is duplicated: {normalizedPath}");
            }

            string resolvedPath = ResolveInsideRoot(root, normalizedPath, errors, $"{prefix}.path");
            if (!File.Exists(resolvedPath))
            {
                errors.Add($"{prefix}.path does not exist: {normalizedPath}");
            }
        }

        ValidateAllowed(asset.Category, Categories, "category", prefix, errors);
        ValidateAllowed(asset.Scope, Scopes, "scope", prefix, errors);
        ValidateAllowed(asset.Origin, Origins, "origin", prefix, errors);
        ValidateAllowed(asset.ReviewStatus, ReviewStatuses, "reviewStatus", prefix, errors);
        ValidateDate(asset.AcquiredOn, "acquiredOn", prefix, errors);
        ValidateDate(asset.ReviewedOn, "reviewedOn", prefix, errors);
        ValidateProof(root, asset.ProofLocation, prefix, errors);

        if (asset.Scope == "production" && (asset.ReviewStatus != "approved" || !asset.DistributionAllowed))
        {
            errors.Add($"{prefix} is a production asset without approved distribution permission.");
        }

        if (asset.Scope == "reference" && (asset.ReviewStatus != "reference-only" || asset.DistributionAllowed))
        {
            errors.Add($"{prefix} is a reference asset but is not blocked from distribution.");
        }
    }

    private static IEnumerable<string> DiscoverRetainedAssets(string root)
    {
        foreach (string relativeRoot in new[] { "assets/reference", "assets/production" })
        {
            string absoluteRoot = Path.Combine(root, relativeRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absoluteRoot))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(absoluteRoot, "*", SearchOption.AllDirectories))
            {
                if (AssetExtensions.Contains(Path.GetExtension(file)))
                {
                    yield return Path.GetRelativePath(root, file).Replace('\\', '/');
                }
            }
        }
    }

    private static string ResolveInsideRoot(string root, string relativePath, List<string> errors, string field)
    {
        string resolved = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{field} escapes the repository root.");
        }

        return resolved;
    }

    private static string? NormalizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Replace('\\', '/').TrimStart('/');
    }

    private static void Require(string? value, string field, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{prefix}.{field} is required.");
        }
    }

    private static void ValidateAllowed(string? value, HashSet<string> allowed, string field, string prefix, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && !allowed.Contains(value))
        {
            errors.Add($"{prefix}.{field} has unsupported value '{value}'.");
        }
    }

    private static void ValidateDate(string? value, string field, string prefix, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) || value.Length != 10))
        {
            errors.Add($"{prefix}.{field} must use YYYY-MM-DD.");
        }
    }

    private static void ValidateProof(string root, string? value, string prefix, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value) || Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri.Scheme is "https" or "http")
        {
            return;
        }

        string proofPath = ResolveInsideRoot(root, value, errors, $"{prefix}.proofLocation");
        if (!File.Exists(proofPath))
        {
            errors.Add($"{prefix}.proofLocation does not exist: {value}");
        }
    }
}

