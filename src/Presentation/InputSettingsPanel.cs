using Godot;
using WormholeWorlds.Application.Input;
using WormholeWorlds.Infrastructure.Input;

namespace WormholeWorlds.Presentation;

public partial class InputSettingsPanel : PanelContainer
{
    private readonly Dictionary<(string Action, InputDeviceKind Device), Button> _bindingButtons = new();
    private VBoxContainer _actionRows = null!;
    private Label _status = null!;
    private GodotInputBindingService? _service;
    private string? _capturingAction;
    private InputDeviceKind _capturingDevice;

    public event Action? CloseRequested;

    public override void _Ready()
    {
        _actionRows = GetNode<VBoxContainer>("Margin/Layout/ActionScroll/ActionRows");
        _status = GetNode<Label>("Margin/Layout/Status");
        GetNode<Button>("Margin/Layout/Footer/ResetButton").Pressed += ResetDefaults;
        GetNode<Button>("Margin/Layout/Footer/CloseButton").Pressed += RequestClose;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_capturingAction is null || _service is null)
        {
            return;
        }

        if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
        {
            CancelCapture("Binding capture cancelled.");
            GetViewport().SetInputAsHandled();
            return;
        }

        if (!GodotInputBindingService.TryDescribeEvent(
                inputEvent,
                _capturingAction,
                _capturingDevice,
                out InputBindingDescriptor? descriptor))
        {
            return;
        }

        if (_service.TryReplaceBinding(_capturingAction, _capturingDevice, descriptor!, out string? error))
        {
            CancelCapture("Binding saved.");
            RefreshBindingLabels();
        }
        else
        {
            _status.Text = error;
        }

        GetViewport().SetInputAsHandled();
    }

    public void Initialize(GodotInputBindingService service)
    {
        _service = service;
        BuildRows();
        RefreshBindingLabels();
    }

    public void FocusFirstBinding()
    {
        _bindingButtons.Values.FirstOrDefault()?.GrabFocus();
    }

    private void BuildRows()
    {
        foreach (Node child in _actionRows.GetChildren())
        {
            child.QueueFree();
        }

        _bindingButtons.Clear();
        foreach (InputActionDefinition action in InputActionCatalog.Actions)
        {
            HBoxContainer row = new() { CustomMinimumSize = new Vector2(0, 42) };
            Label label = new()
            {
                Text = action.DisplayName,
                CustomMinimumSize = new Vector2(180, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            row.AddChild(label);

            AddBindingButton(row, action, InputDeviceKind.KeyboardMouse, "Keyboard / mouse");
            AddBindingButton(row, action, InputDeviceKind.Controller, "Controller");

            Button clearKeyboard = new() { Text = "Clear keys" };
            clearKeyboard.Pressed += () => Clear(action.Name, InputDeviceKind.KeyboardMouse);
            row.AddChild(clearKeyboard);

            Button clearController = new() { Text = "Clear pad" };
            clearController.Pressed += () => Clear(action.Name, InputDeviceKind.Controller);
            row.AddChild(clearController);

            _actionRows.AddChild(row);
        }
    }

    private void AddBindingButton(
        HBoxContainer row,
        InputActionDefinition action,
        InputDeviceKind device,
        string accessibleDeviceName)
    {
        Button button = new() { CustomMinimumSize = new Vector2(190, 0) };
        button.TooltipText = $"Rebind {action.DisplayName} for {accessibleDeviceName}";
        button.Pressed += () => BeginCapture(action.Name, action.DisplayName, device, accessibleDeviceName);
        row.AddChild(button);
        _bindingButtons[(action.Name, device)] = button;
    }

    private void BeginCapture(string action, string displayName, InputDeviceKind device, string deviceName)
    {
        _capturingAction = action;
        _capturingDevice = device;
        _status.Text = $"Press a {deviceName} input for {displayName}. Escape cancels.";
    }

    private void CancelCapture(string status)
    {
        _capturingAction = null;
        _status.Text = status;
    }

    private void Clear(string action, InputDeviceKind device)
    {
        if (_service is null)
        {
            return;
        }

        _status.Text = _service.TryClearBindings(action, device, out string? error)
            ? "Binding cleared."
            : error;
        RefreshBindingLabels();
    }

    private void ResetDefaults()
    {
        _service?.ResetDefaults();
        CancelCapture("Default bindings restored.");
        RefreshBindingLabels();
    }

    private void RefreshBindingLabels()
    {
        if (_service is null)
        {
            return;
        }

        foreach (((string action, InputDeviceKind device), Button button) in _bindingButtons)
        {
            button.Text = _service.DescribeBindings(action, device);
        }
    }

    private void RequestClose()
    {
        CancelCapture("Select a binding to change it.");
        CloseRequested?.Invoke();
    }
}
