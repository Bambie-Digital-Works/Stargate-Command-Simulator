using FacilityCommand.Application.Configuration;
using FacilityCommand.Application.Diagnostics;
using FacilityCommand.Application.Input;
using FacilityCommand.Application.Logging;
using FacilityCommand.Application.Operations;
using FacilityCommand.Infrastructure.Input;
using Godot;

namespace FacilityCommand.Presentation;

public partial class OperatorShell : Control
{
    private Label _titleLabel = null!;
    private Label _statusLabel = null!;
    private DiagnosticsOverlay _diagnosticsOverlay = null!;
    private Button _diagnosticsButton = null!;
    private Button _inputSettingsButton = null!;
    private InputSettingsPanel _inputSettingsPanel = null!;
    private FocusCoordinator _focusCoordinator = null!;
    private OperationsBoard _operationsBoard = null!;
    private TransitControl _transitControlPanel = null!;
    private SurveyTelemetry _surveyTelemetryPanel = null!;
    private ReturnControl _returnControlPanel = null!;
    private ExpeditionRoster _expeditionRosterPanel = null!;
    private OperationsBoardService? _operations;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("SafeArea/Layout/Title");
        _statusLabel = GetNode<Label>("SafeArea/Layout/Status");
        _diagnosticsOverlay = GetNode<DiagnosticsOverlay>("SafeArea/Layout/Workspace/DiagnosticsOverlay");
        _diagnosticsButton = GetNode<Button>("SafeArea/Layout/Toolbar/DiagnosticsButton");
        _inputSettingsButton = GetNode<Button>("SafeArea/Layout/Toolbar/InputSettingsButton");
        _inputSettingsPanel = GetNode<InputSettingsPanel>("SafeArea/Layout/Workspace/InputSettingsPanel");
        _focusCoordinator = GetNode<FocusCoordinator>("FocusCoordinator");
        _operationsBoard = GetNode<OperationsBoard>("SafeArea/Layout/Workspace/NavigationHost/OperationsBoard");
        _transitControlPanel = GetNode<TransitControl>("SafeArea/Layout/Workspace/NavigationHost/TransitControlPanel");
        _surveyTelemetryPanel = GetNode<SurveyTelemetry>("SafeArea/Layout/Workspace/NavigationHost/SurveyTelemetryPanel");
        _returnControlPanel = GetNode<ReturnControl>("SafeArea/Layout/Workspace/NavigationHost/ReturnControlPanel");
        _expeditionRosterPanel = GetNode<ExpeditionRoster>("SafeArea/Layout/Workspace/NavigationHost/ExpeditionRosterPanel");

        _diagnosticsButton.Pressed += ToggleDiagnostics;
        _inputSettingsButton.Pressed += ShowInputSettings;
        _inputSettingsPanel.CloseRequested += HideInputSettings;
        _operationsBoard.ScreenRequested += NavigateTo;
        _transitControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _transitControlPanel.StateChanged += RefreshShellStatus;
        _surveyTelemetryPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _surveyTelemetryPanel.StateChanged += RefreshShellStatus;
        _returnControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _returnControlPanel.StateChanged += RefreshShellStatus;
        _expeditionRosterPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _expeditionRosterPanel.StateChanged += RefreshShellStatus;
        _diagnosticsButton.GrabFocus();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent.IsActionPressed(InputActionCatalog.ToggleDiagnostics))
        {
            ToggleDiagnostics();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_inputSettingsPanel.Visible && inputEvent.IsActionPressed(InputActionCatalog.Back))
        {
            HideInputSettings();
            GetViewport().SetInputAsHandled();
            return;
        }

        if (_operations is null || _inputSettingsPanel.Visible)
        {
            return;
        }

        if (inputEvent.IsActionPressed(InputActionCatalog.NextPanel))
        {
            NavigateTo(_operations.CycleScreen(1));
            GetViewport().SetInputAsHandled();
        }
        else if (inputEvent.IsActionPressed(InputActionCatalog.PreviousPanel))
        {
            NavigateTo(_operations.CycleScreen(-1));
            GetViewport().SetInputAsHandled();
        }
        else if (_operations.ActiveScreen != OperatorConsoleScreen.OperationsBoard
            && inputEvent.IsActionPressed(InputActionCatalog.Back))
        {
            NavigateTo(OperatorConsoleScreen.OperationsBoard);
            GetViewport().SetInputAsHandled();
        }
    }

    public void Initialize(
        BuildMetadata metadata,
        AppConfiguration configuration,
        IApplicationLogger logger,
        GodotInputBindingService inputBindings,
        OperationsBoardService operations)
    {
        _operations = operations;
        _diagnosticsOverlay.Initialize(metadata, logger);
        _diagnosticsOverlay.Visible = configuration.Diagnostics.OverlayVisibleOnStartup;
        _inputSettingsPanel.Initialize(inputBindings);
        _inputSettingsPanel.Visible = false;
        _focusCoordinator.Initialize(GetNode<Label>("SafeArea/Layout/Toolbar/InputModeLabel"), logger);
        _operationsBoard.Initialize(operations);
        _transitControlPanel.Initialize(operations.Transit, operations.Clock);
        _surveyTelemetryPanel.Initialize(operations.Survey, operations.Clock);
        _returnControlPanel.Initialize(operations.Security, operations.Transit, operations.Clock);
        _expeditionRosterPanel.Initialize(operations.Roster, operations.Transit, operations.Clock);
        _titleLabel.Text = "Operations Board online";
        ApplyScreen(operations.ActiveScreen, grabFocus: false);
        UpdateDiagnosticsButtonText();
    }

    private void NavigateTo(OperatorConsoleScreen screen)
    {
        if (_operations is null)
        {
            return;
        }

        _operations.SetActiveScreen(screen);
        ApplyScreen(screen, grabFocus: true);
    }

    private void ApplyScreen(OperatorConsoleScreen screen, bool grabFocus)
    {
        if (_operations is null)
        {
            return;
        }

        _operationsBoard.Visible = screen == OperatorConsoleScreen.OperationsBoard;
        _transitControlPanel.Visible = screen == OperatorConsoleScreen.TransitControl;
        _surveyTelemetryPanel.Visible = screen == OperatorConsoleScreen.SurveyTelemetry;
        _returnControlPanel.Visible = screen == OperatorConsoleScreen.ReturnControl;
        _expeditionRosterPanel.Visible = screen == OperatorConsoleScreen.ExpeditionRoster;

        if (screen == OperatorConsoleScreen.OperationsBoard)
        {
            _operationsBoard.Refresh();
        }
        else if (screen == OperatorConsoleScreen.TransitControl)
        {
            _transitControlPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.SurveyTelemetry)
        {
            _surveyTelemetryPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.ReturnControl)
        {
            _returnControlPanel.Refresh();
        }
        else if (screen == OperatorConsoleScreen.ExpeditionRoster)
        {
            _expeditionRosterPanel.Refresh();
        }

        RefreshShellStatus();
        _titleLabel.Text = screen switch
        {
            OperatorConsoleScreen.OperationsBoard => "Operations Board online",
            OperatorConsoleScreen.TransitControl => "Transit Control",
            OperatorConsoleScreen.SurveyTelemetry => "Survey Telemetry",
            OperatorConsoleScreen.ReturnControl => "Return Control",
            OperatorConsoleScreen.ExpeditionRoster => "Expedition Roster",
            _ => "Operator systems standing by",
        };

        if (!grabFocus)
        {
            return;
        }

        switch (screen)
        {
            case OperatorConsoleScreen.OperationsBoard:
                _operationsBoard.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.TransitControl:
                _transitControlPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.SurveyTelemetry:
                _surveyTelemetryPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ReturnControl:
                _returnControlPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ExpeditionRoster:
                _expeditionRosterPanel.FocusPrimaryAction();
                break;
        }
    }

    private void RefreshShellStatus()
    {
        if (_operations is null)
        {
            return;
        }

        _statusLabel.Text = _operations.GetReadModel().AnnouncementSummary;
        if (_operationsBoard.Visible)
        {
            _operationsBoard.Refresh();
        }

        if (_surveyTelemetryPanel.Visible)
        {
            _surveyTelemetryPanel.Refresh();
        }

        if (_returnControlPanel.Visible)
        {
            _returnControlPanel.Refresh();
        }

        if (_expeditionRosterPanel.Visible)
        {
            _expeditionRosterPanel.Refresh();
        }
    }

    private void ShowInputSettings()
    {
        _inputSettingsPanel.Visible = true;
        _inputSettingsPanel.FocusFirstBinding();
    }

    private void HideInputSettings()
    {
        _inputSettingsPanel.Visible = false;
        _inputSettingsButton.GrabFocus();
    }

    private void ToggleDiagnostics()
    {
        _diagnosticsOverlay.Visible = !_diagnosticsOverlay.Visible;
        _diagnosticsOverlay.RefreshRecentEvents();
        UpdateDiagnosticsButtonText();
    }

    private void UpdateDiagnosticsButtonText()
    {
        _diagnosticsButton.Text = _diagnosticsOverlay.Visible ? "Hide diagnostics" : "Show diagnostics";
    }
}
