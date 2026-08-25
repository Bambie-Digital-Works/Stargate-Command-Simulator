using FacilityCommand.Tools.AssetLedgerValidator;

namespace FacilityCommand.Tests;

public sealed class AssetLedgerValidationTests
{
    [Fact]
    public void CurrentRepositoryLedgerPasses()
    {
        string root = FindRepositoryRoot();

        IReadOnlyList<string> errors = AssetLedgerValidation.Validate(root);

        Assert.Empty(errors);
    }

    [Fact]
    public void RejectsUnrecordedAndUnapprovedProductionAssets()
    {
        using TestDirectory directory = new();
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "assets", "production"));
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "assets", "reference"));
        File.WriteAllBytes(System.IO.Path.Combine(directory.Path, "assets", "production", "unrecorded.png"), [0]);
        File.WriteAllBytes(System.IO.Path.Combine(directory.Path, "assets", "production", "pending.wav"), [0]);
        string ledger = """
            {
              "schemaVersion": 1,
              "assets": [{
                "id": "production.pending",
                "path": "assets/production/pending.wav",
                "category": "audio",
                "scope": "production",
                "origin": "original",
                "creator": "Owner",
                "sourceUrl": "https://example.invalid/source",
                "licenseExpression": "Proprietary",
                "acquiredOn": "2026-08-25",
                "modifications": "None",
                "attribution": "None required",
                "proofLocation": "https://example.invalid/proof",
                "reviewStatus": "unverified",
                "reviewedBy": "Owner",
                "reviewedOn": "2026-08-25",
                "distributionAllowed": false
              }]
            }
            """;
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "assets"));
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "assets", "asset-ledger.json"), ledger);

        IReadOnlyList<string> errors = AssetLedgerValidation.Validate(directory.Path);

        Assert.Contains(errors, error => error.Contains("without approved distribution", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("unrecorded.png", StringComparison.Ordinal));
    }

    [Fact]
    public void RejectsMissingProvenanceFileAndUnsupportedGovernanceValues()
    {
        using TestDirectory directory = new();
        Directory.CreateDirectory(System.IO.Path.Combine(directory.Path, "assets"));
        const string ledger = """
            {
              "schemaVersion": 1,
              "assets": [{
                "id": "production.invalid",
                "path": "assets/production/missing.png",
                "category": "image",
                "scope": "production",
                "origin": "original",
                "creator": "",
                "sourceUrl": "https://example.invalid/source",
                "licenseExpression": "Definitely-Not-A-License",
                "acquiredOn": "2026-08-25",
                "modifications": "None",
                "attribution": "None required",
                "proofLocation": "https://example.invalid/proof",
                "reviewStatus": "ready",
                "reviewedBy": "Owner",
                "reviewedOn": "2026-08-25",
                "distributionAllowed": true
              }]
            }
            """;
        File.WriteAllText(System.IO.Path.Combine(directory.Path, "assets", "asset-ledger.json"), ledger);

        IReadOnlyList<string> errors = AssetLedgerValidation.Validate(directory.Path);

        Assert.Contains(errors, error => error.Contains("creator is required", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("path does not exist", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("licenseExpression has unsupported value", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("reviewStatus has unsupported value", StringComparison.Ordinal));
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(System.IO.Path.Combine(current.FullName, "project.godot")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
