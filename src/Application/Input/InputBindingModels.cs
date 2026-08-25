using System.Text.Json.Serialization;

namespace FacilityCommand.Application.Input;

public enum InputDeviceKind
{
    KeyboardMouse,
    Controller,
}

public enum InputBindingKind
{
    Key,
    MouseButton,
    JoyButton,
    JoyAxis,
}

public sealed record InputBindingDescriptor(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("device")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<InputDeviceKind>))]
    InputDeviceKind Device,
    [property: JsonPropertyName("kind")]
    [property: JsonConverter(typeof(JsonStringEnumConverter<InputBindingKind>))]
    InputBindingKind Kind,
    [property: JsonPropertyName("code")] long Code,
    [property: JsonPropertyName("axisValue")] float AxisValue = 0,
    [property: JsonPropertyName("ctrl")] bool Ctrl = false,
    [property: JsonPropertyName("alt")] bool Alt = false,
    [property: JsonPropertyName("shift")] bool Shift = false,
    [property: JsonPropertyName("meta")] bool Meta = false);

public sealed record InputBindingsDocument(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("bindings")] IReadOnlyList<InputBindingDescriptor> Bindings);

public sealed record InputBindingLoadResult(
    IReadOnlyList<InputBindingDescriptor> Bindings,
    bool UsedDefaults,
    string? Warning);

