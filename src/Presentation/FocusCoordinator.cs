using FacilityCommand.Application.Logging;
using Godot;

namespace FacilityCommand.Presentation;

public partial class FocusCoordinator : Node
{
    private Control? _lastFocused;
    private Label? _inputModeLabel;
    private IApplicationLogger? _logger;
    private string _inputMode = "Keyboard / mouse";

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
        string nextMode = inputEvent switch
        {
            InputEventJoypadButton => "Controller",
            InputEventJoypadMotion motion when Math.Abs(motion.AxisValue) >= 0.5f => "Controller",
            InputEventKey => "Keyboard / mouse",
            InputEventMouseButton => "Keyboard / mouse",
            InputEventMouseMotion => "Keyboard / mouse",
            _ => _inputMode,
        };

        if (nextMode != _inputMode)
        {
            _inputMode = nextMode;
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

        CallDeferred(MethodName.RestoreFocus);
    }

    private void UpdateInputModeLabel()
    {
        if (_inputModeLabel is not null)
        {
            _inputModeLabel.Text = $"Active input: {_inputMode}";
        }
    }
}
