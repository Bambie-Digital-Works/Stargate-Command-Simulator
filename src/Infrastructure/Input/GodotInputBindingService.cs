using FacilityCommand.Application.Input;
using FacilityCommand.Application.Logging;
using Godot;

namespace FacilityCommand.Infrastructure.Input;

public sealed class GodotInputBindingService
{
    private readonly InputBindingStore _store;
    private readonly IApplicationLogger _logger;
    private List<InputBindingDescriptor> _bindings = [];

    public GodotInputBindingService(InputBindingStore store, IApplicationLogger logger)
    {
        _store = store;
        _logger = logger;
    }

    public IReadOnlyList<InputBindingDescriptor> Bindings => _bindings;

    public void Initialize()
    {
        InputBindingLoadResult result = _store.LoadOrDefault(InputActionCatalog.Defaults);
        _bindings = result.Bindings.ToList();
        ApplyBindings();

        if (result.Warning is not null)
        {
            _logger.Log(
                ApplicationLogLevel.Warning,
                "input.bindings_recovered",
                result.Warning,
                new Dictionary<string, string> { ["component"] = "input" });
        }
    }

    public bool TryReplaceBinding(
        string action,
        InputDeviceKind device,
        InputBindingDescriptor replacement,
        out string? error)
    {
        if (!InputActionCatalog.IsKnown(action) || replacement.Action != action || replacement.Device != device)
        {
            error = "The captured input does not match the selected action and device.";
            return false;
        }

        InputBindingDescriptor? collision = _bindings.FirstOrDefault(binding =>
            binding.Action != action && SamePhysicalInput(binding, replacement));
        if (collision is not null)
        {
            string displayName = InputActionCatalog.Actions.First(item => item.Name == collision.Action).DisplayName;
            error = $"That input is already assigned to {displayName}.";
            return false;
        }

        _bindings.RemoveAll(binding => binding.Action == action && binding.Device == device);
        _bindings.Add(replacement);
        SaveAndApply();
        error = null;
        return true;
    }

    public bool TryClearBindings(string action, InputDeviceKind device, out string? error)
    {
        List<InputBindingDescriptor> candidate = _bindings
            .Where(binding => binding.Action != action || binding.Device != device)
            .ToList();

        if (InputActionCatalog.IsRequired(action) && !candidate.Any(binding => binding.Action == action))
        {
            error = "Accept and Back must always retain at least one binding.";
            return false;
        }

        _bindings = candidate;
        SaveAndApply();
        error = null;
        return true;
    }

    public void ResetDefaults()
    {
        _bindings = InputActionCatalog.Defaults.ToList();
        SaveAndApply();
    }

    public string DescribeBindings(string action, InputDeviceKind device)
    {
        string[] descriptions = _bindings
            .Where(binding => binding.Action == action && binding.Device == device)
            .Select(DescribeBinding)
            .ToArray();
        return descriptions.Length == 0 ? "Unbound" : string.Join(" / ", descriptions);
    }

    public static bool TryDescribeEvent(
        InputEvent inputEvent,
        string action,
        InputDeviceKind targetDevice,
        out InputBindingDescriptor? descriptor)
    {
        descriptor = inputEvent switch
        {
            InputEventKey key when targetDevice == InputDeviceKind.KeyboardMouse && key.Pressed && !key.Echo =>
                new InputBindingDescriptor(
                    action,
                    targetDevice,
                    InputBindingKind.Key,
                    (long)(key.PhysicalKeycode != Key.None ? key.PhysicalKeycode : key.Keycode),
                    Ctrl: key.CtrlPressed,
                    Alt: key.AltPressed,
                    Shift: key.ShiftPressed,
                    Meta: key.MetaPressed),
            InputEventMouseButton mouse when targetDevice == InputDeviceKind.KeyboardMouse && mouse.Pressed =>
                new InputBindingDescriptor(action, targetDevice, InputBindingKind.MouseButton, (long)mouse.ButtonIndex),
            InputEventJoypadButton button when targetDevice == InputDeviceKind.Controller && button.Pressed =>
                new InputBindingDescriptor(action, targetDevice, InputBindingKind.JoyButton, (long)button.ButtonIndex),
            InputEventJoypadMotion motion when targetDevice == InputDeviceKind.Controller && Math.Abs(motion.AxisValue) >= 0.7f =>
                new InputBindingDescriptor(action, targetDevice, InputBindingKind.JoyAxis, (long)motion.Axis, Math.Sign(motion.AxisValue)),
            _ => null,
        };

        return descriptor is not null;
    }

    private void SaveAndApply()
    {
        _store.Save(_bindings);
        ApplyBindings();
        _logger.Log(
            ApplicationLogLevel.Information,
            "input.bindings_changed",
            "Input bindings were updated.",
            new Dictionary<string, string> { ["component"] = "input" });
    }

    private void ApplyBindings()
    {
        foreach (InputActionDefinition action in InputActionCatalog.Actions)
        {
            StringName actionName = action.Name;
            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName, 0.5f);
            }

            InputMap.ActionEraseEvents(actionName);
        }

        foreach (InputBindingDescriptor binding in _bindings)
        {
            InputMap.ActionAddEvent(binding.Action, CreateEvent(binding));
        }
    }

    private static InputEvent CreateEvent(InputBindingDescriptor binding) => binding.Kind switch
    {
        InputBindingKind.Key => new InputEventKey
        {
            PhysicalKeycode = (Key)binding.Code,
            CtrlPressed = binding.Ctrl,
            AltPressed = binding.Alt,
            ShiftPressed = binding.Shift,
            MetaPressed = binding.Meta,
        },
        InputBindingKind.MouseButton => new InputEventMouseButton { ButtonIndex = (MouseButton)binding.Code },
        InputBindingKind.JoyButton => new InputEventJoypadButton { ButtonIndex = (JoyButton)binding.Code },
        InputBindingKind.JoyAxis => new InputEventJoypadMotion
        {
            Axis = (JoyAxis)binding.Code,
            AxisValue = binding.AxisValue,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(binding)),
    };

    private static bool SamePhysicalInput(InputBindingDescriptor left, InputBindingDescriptor right) =>
        left.Device == right.Device &&
        left.Kind == right.Kind &&
        left.Code == right.Code &&
        left.AxisValue == right.AxisValue &&
        left.Ctrl == right.Ctrl &&
        left.Alt == right.Alt &&
        left.Shift == right.Shift &&
        left.Meta == right.Meta;

    private static string DescribeBinding(InputBindingDescriptor binding) => binding.Kind switch
    {
        InputBindingKind.Key => OS.GetKeycodeString((Key)binding.Code),
        InputBindingKind.MouseButton => $"Mouse {(MouseButton)binding.Code}",
        InputBindingKind.JoyButton => $"Controller {(JoyButton)binding.Code}",
        InputBindingKind.JoyAxis => $"Controller {(JoyAxis)binding.Code} {(binding.AxisValue < 0 ? "−" : "+")}",
        _ => "Unknown",
    };
}

