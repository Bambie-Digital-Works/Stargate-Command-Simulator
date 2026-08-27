using System.Text.Json;
using WormholeWorlds.Application.Input;

namespace WormholeWorlds.Infrastructure.Input;

public sealed class InputBindingStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    private readonly string _path;

    public InputBindingStore(string path)
    {
        _path = path;
    }

    public InputBindingLoadResult LoadOrDefault(IReadOnlyList<InputBindingDescriptor> defaults)
    {
        if (!File.Exists(_path))
        {
            return new InputBindingLoadResult(defaults.ToArray(), true, null);
        }

        try
        {
            InputBindingsDocument? document = JsonSerializer.Deserialize<InputBindingsDocument>(File.ReadAllText(_path), JsonOptions);
            if (document is null || document.SchemaVersion != 1)
            {
                return Recover(defaults, "Input bindings use an unsupported schema.");
            }

            IReadOnlyList<string> errors = Validate(document.Bindings);
            return errors.Count == 0
                ? new InputBindingLoadResult(document.Bindings.ToArray(), false, null)
                : Recover(defaults, $"Input bindings are invalid: {errors[0]}");
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return Recover(defaults, "Input bindings could not be read and were reset to defaults.");
        }
    }

    public void Save(IReadOnlyList<InputBindingDescriptor> bindings)
    {
        IReadOnlyList<string> errors = Validate(bindings);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Cannot save invalid input bindings: {errors[0]}");
        }

        string? directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temporaryPath = _path + ".tmp";
        string json = JsonSerializer.Serialize(new InputBindingsDocument(1, bindings), JsonOptions);
        File.WriteAllText(temporaryPath, json);
        File.Move(temporaryPath, _path, true);
    }

    public static IReadOnlyList<string> Validate(IReadOnlyList<InputBindingDescriptor>? bindings)
    {
        List<string> errors = [];
        if (bindings is null)
        {
            return ["bindings is required."];
        }

        HashSet<InputBindingDescriptor> uniqueBindings = [];
        List<InputBindingDescriptor> validatedBindings = [];
        foreach (InputBindingDescriptor binding in bindings)
        {
            if (!InputActionCatalog.IsKnown(binding.Action))
            {
                errors.Add($"Unknown action '{binding.Action}'.");
            }

            if (!Enum.IsDefined(binding.Device) || !Enum.IsDefined(binding.Kind))
            {
                errors.Add($"Action '{binding.Action}' has an unsupported binding type.");
            }

            bool deviceMatches = binding.Device switch
            {
                InputDeviceKind.KeyboardMouse => binding.Kind is InputBindingKind.Key or InputBindingKind.MouseButton,
                InputDeviceKind.Controller => binding.Kind is InputBindingKind.JoyButton or InputBindingKind.JoyAxis,
                _ => false,
            };
            if (!deviceMatches)
            {
                errors.Add($"Action '{binding.Action}' has a binding that does not match its device.");
            }

            if (binding.Kind == InputBindingKind.JoyAxis && Math.Abs(binding.AxisValue) != 1)
            {
                errors.Add($"Action '{binding.Action}' has an invalid controller axis direction.");
            }

            if (!uniqueBindings.Add(binding))
            {
                errors.Add($"Action '{binding.Action}' contains a duplicate binding.");
            }

            InputBindingDescriptor? collision = validatedBindings.FirstOrDefault(existing =>
                existing.Action != binding.Action && SamePhysicalInput(existing, binding));
            if (collision is not null)
            {
                errors.Add($"Actions '{collision.Action}' and '{binding.Action}' use the same input.");
            }

            validatedBindings.Add(binding);
        }

        foreach (InputActionDefinition required in InputActionCatalog.Actions.Where(action => action.Required))
        {
            if (!bindings.Any(binding => binding.Action == required.Name))
            {
                errors.Add($"Required action '{required.Name}' has no binding.");
            }
        }

        return errors;
    }

    private static bool SamePhysicalInput(InputBindingDescriptor left, InputBindingDescriptor right) =>
        left.Device == right.Device &&
        left.Kind == right.Kind &&
        left.Code == right.Code &&
        left.AxisValue == right.AxisValue &&
        left.Ctrl == right.Ctrl &&
        left.Alt == right.Alt &&
        left.Shift == right.Shift &&
        left.Meta == right.Meta;

    private InputBindingLoadResult Recover(IReadOnlyList<InputBindingDescriptor> defaults, string warning)
    {
        try
        {
            string backupPath = _path + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}.json";
            File.Copy(_path, backupPath, true);
        }
        catch (IOException)
        {
            warning += " The invalid file could not be backed up.";
        }

        return new InputBindingLoadResult(defaults.ToArray(), true, warning);
    }
}
