using Godot;
using WormholeWorlds.Application.Input;
using WormholeWorlds.Application.Logging;

namespace WormholeWorlds.Presentation;

public partial class FocusCoordinator : Node
{
    private Control? _lastFocused;
    private Label? _inputModeLabel;
    private IApplicationLogger? _logger;
    private readonly InputDeviceState _deviceState = new();

    public override void _Ready()
    {
        GetViewport().GuiFocusChanged += OnFocusChanged;
        Input.JoyConnectionChanged += OnJoyConnectionChanged;
    }

    public override void _ExitTree()
    {
        GetViewport().GuiFocusChanged -= OnFocusChanged;
        Input.JoyConnectionChanged -= OnJoyConnectionChanged;
    }

    public override void _Input(InputEvent inputEvent)
    {
        InputDeviceKind? nextDevice = inputEvent switch
        {
            InputEventJoypadButton => InputDeviceKind.Controller,
            InputEventJoypadMotion motion when Math.Abs(motion.AxisValue) >= 0.5f => InputDeviceKind.Controller,
            InputEventKey => InputDeviceKind.KeyboardMouse,
            InputEventMouseButton => InputDeviceKind.KeyboardMouse,
            InputEventMouseMotion => InputDeviceKind.KeyboardMouse,
            _ => null,
        };

        if (nextDevice is not null && _deviceState.SetActive(nextDevice.Value))
        {
            UpdateInputModeLabel();
        }
    }

    public void Initialize(Label inputModeLabel, IApplicationLogger logger)
    {
        _inputModeLabel = inputModeLabel;
        _logger = logger;
        UpdateInputModeLabel();
    }

    public void RestoreFocus()
    {
        if (_lastFocused is { Visible: true, FocusMode: not Control.FocusModeEnum.None } && _lastFocused.IsInsideTree())
        {
            _lastFocused.GrabFocus();
        }
    }

    private void OnFocusChanged(Control? control)
    {
        if (control is not null)
        {
            _lastFocused = control;
        }
    }

    private void OnJoyConnectionChanged(long device, bool connected)
    {
        _logger?.Log(
            ApplicationLogLevel.Information,
            "input.controller_connection_changed",
            connected ? "Controller connected." : "Controller disconnected.",
            new Dictionary<string, string>
            {
                ["component"] = "input",
                ["device"] = device.ToString(),
                ["state"] = connected ? "connected" : "disconnected",
            });

        if (!connected)
        {
            _deviceState.ControllerDisconnected();
            UpdateInputModeLabel();
        }

        CallDeferred(MethodName.RestoreFocus);
    }

    private void UpdateInputModeLabel()
    {
        if (_inputModeLabel is not null)
        {
            string displayName = _deviceState.ActiveDevice == InputDeviceKind.Controller
                ? "Controller"
                : "Keyboard / mouse";
            _inputModeLabel.Text = $"Active input: {displayName}";
        }
    }
}
