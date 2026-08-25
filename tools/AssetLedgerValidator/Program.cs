using FacilityCommand.Tools.AssetLedgerValidator;

string root = Directory.GetCurrentDirectory();
for (int index = 0; index < args.Length; index++)
{
    if (args[index] == "--root" && index + 1 < args.Length)
    {
        root = args[++index];
        continue;
    }

    Console.Error.WriteLine($"Unknown or incomplete argument: {args[index]}");
    return 2;
}

IReadOnlyList<string> errors = AssetLedgerValidation.Validate(root);
if (errors.Count > 0)
{
    Console.Error.WriteLine($"Asset ledger validation failed with {errors.Count} error(s):");
    foreach (string error in errors)
    {
        Console.Error.WriteLine($"- {error}");
    }

    return 1;
}

Console.WriteLine("Asset ledger validation passed.");
return 0;

