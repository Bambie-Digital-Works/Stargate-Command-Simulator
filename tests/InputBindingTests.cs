using WormholeWorlds.Application.Input;
using WormholeWorlds.Infrastructure.Input;

namespace WormholeWorlds.Tests;

public sealed class InputBindingTests
{
    [Fact]
    public void DefaultsAreValidAndCoverBothDevices()
    {
        IReadOnlyList<string> errors = InputBindingStore.Validate(InputActionCatalog.Defaults);

        Assert.Empty(errors);
        foreach (InputActionDefinition action in InputActionCatalog.Actions)
        {
            Assert.Contains(InputActionCatalog.Defaults, binding => binding.Action == action.Name && binding.Device == InputDeviceKind.KeyboardMouse);
            Assert.Contains(InputActionCatalog.Defaults, binding => binding.Action == action.Name && binding.Device == InputDeviceKind.Controller);
        }
    }

    [Fact]
    public void SavesAndLoadsBindingsAtomically()
    {
        using TestDirectory directory = new();
        string path = System.IO.Path.Combine(directory.Path, "input_bindings.v1.json");
        InputBindingStore store = new(path);

        store.Save(InputActionCatalog.Defaults);
        InputBindingLoadResult loaded = store.LoadOrDefault([]);

        Assert.False(loaded.UsedDefaults);
        Assert.Equal(InputActionCatalog.Defaults.Count, loaded.Bindings.Count);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void CorruptFileIsBackedUpAndDefaultsAreRestored()
    {
        using TestDirectory directory = new();
        string path = System.IO.Path.Combine(directory.Path, "input_bindings.v1.json");
        File.WriteAllText(path, "{ not json }");
        InputBindingStore store = new(path);

        InputBindingLoadResult loaded = store.LoadOrDefault(InputActionCatalog.Defaults);

        Assert.True(loaded.UsedDefaults);
        Assert.NotNull(loaded.Warning);
        Assert.Equal(InputActionCatalog.Defaults.Count, loaded.Bindings.Count);
        Assert.Single(Directory.GetFiles(directory.Path, "*.corrupt-*.json"));
    }

    [Fact]
    public void ValidationRejectsCollisionsAndMissingRequiredActions()
    {
        List<InputBindingDescriptor> invalid = InputActionCatalog.Defaults
            .Where(binding => binding.Action != InputActionCatalog.Accept)
            .ToList();
        invalid.Add(new InputBindingDescriptor(
            InputActionCatalog.Pause,
            InputDeviceKind.KeyboardMouse,
            InputBindingKind.Key,
            4194305));

        IReadOnlyList<string> errors = InputBindingStore.Validate(invalid);

        Assert.Contains(errors, error => error.Contains("Required action", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("same input", StringComparison.Ordinal));
    }

    [Fact]
    public void DeviceStateChangesAndFallsBackAfterDisconnect()
    {
        InputDeviceState state = new();

        Assert.False(state.SetActive(InputDeviceKind.KeyboardMouse));
        Assert.True(state.SetActive(InputDeviceKind.Controller));
        Assert.Equal(InputDeviceKind.Controller, state.ActiveDevice);
        Assert.True(state.ControllerDisconnected());
        Assert.Equal(InputDeviceKind.KeyboardMouse, state.ActiveDevice);
    }
}
