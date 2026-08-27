namespace WormholeWorlds.Application.Input;

public sealed record InputActionDefinition(string Name, string DisplayName, bool Required);

public static class InputActionCatalog
{
    public const string NavigateUp = "ui_up";
    public const string NavigateDown = "ui_down";
    public const string NavigateLeft = "ui_left";
    public const string NavigateRight = "ui_right";
    public const string Accept = "ui_accept";
    public const string Back = "ui_cancel";
    public const string Pause = "command_pause";
    public const string ToggleDiagnostics = "command_toggle_diagnostics";
    public const string PreviousPanel = "command_previous_panel";
    public const string NextPanel = "command_next_panel";

    public static readonly IReadOnlyList<InputActionDefinition> Actions =
    [
        new(NavigateUp, "Navigate up", false),
        new(NavigateDown, "Navigate down", false),
        new(NavigateLeft, "Navigate left", false),
        new(NavigateRight, "Navigate right", false),
        new(Accept, "Accept", true),
        new(Back, "Back", true),
        new(Pause, "Pause", false),
        new(ToggleDiagnostics, "Toggle diagnostics", false),
        new(PreviousPanel, "Previous panel", false),
        new(NextPanel, "Next panel", false),
    ];

    public static readonly IReadOnlyList<InputBindingDescriptor> Defaults =
    [
        Key(NavigateUp, 4194320), Key(NavigateUp, 87), JoyButton(NavigateUp, 11), JoyAxis(NavigateUp, 1, -1),
        Key(NavigateDown, 4194322), Key(NavigateDown, 83), JoyButton(NavigateDown, 12), JoyAxis(NavigateDown, 1, 1),
        Key(NavigateLeft, 4194319), Key(NavigateLeft, 65), JoyButton(NavigateLeft, 13), JoyAxis(NavigateLeft, 0, -1),
        Key(NavigateRight, 4194321), Key(NavigateRight, 68), JoyButton(NavigateRight, 14), JoyAxis(NavigateRight, 0, 1),
        Key(Accept, 4194309), Key(Accept, 32), JoyButton(Accept, 0),
        Key(Back, 4194305), JoyButton(Back, 1),
        Key(Pause, 80), JoyButton(Pause, 6),
        Key(ToggleDiagnostics, 4194334), JoyButton(ToggleDiagnostics, 3),
        Key(PreviousPanel, 81), Key(PreviousPanel, 4194323), JoyButton(PreviousPanel, 9),
        Key(NextPanel, 69), Key(NextPanel, 4194324), JoyButton(NextPanel, 10),
    ];

    public static bool IsKnown(string action) => Actions.Any(definition => definition.Name == action);

    public static bool IsRequired(string action) => Actions.Any(definition => definition.Name == action && definition.Required);

    private static InputBindingDescriptor Key(string action, long keyCode) =>
        new(action, InputDeviceKind.KeyboardMouse, InputBindingKind.Key, keyCode);

    private static InputBindingDescriptor JoyButton(string action, long button) =>
        new(action, InputDeviceKind.Controller, InputBindingKind.JoyButton, button);

    private static InputBindingDescriptor JoyAxis(string action, long axis, float direction) =>
        new(action, InputDeviceKind.Controller, InputBindingKind.JoyAxis, axis, direction);
}
