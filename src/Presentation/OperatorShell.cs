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
    private WorkflowPlaceholderPanel _transitControlPanel = null!;
    private WorkflowPlaceholderPanel _returnControlPanel = null!;
    private WorkflowPlaceholderPanel _expeditionRosterPanel = null!;
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
        _transitControlPanel = GetNode<WorkflowPlaceholderPanel>("SafeArea/Layout/Workspace/NavigationHost/TransitControlPanel");
        _returnControlPanel = GetNode<WorkflowPlaceholderPanel>("SafeArea/Layout/Workspace/NavigationHost/ReturnControlPanel");
        _expeditionRosterPanel = GetNode<WorkflowPlaceholderPanel>("SafeArea/Layout/Workspace/NavigationHost/ExpeditionRosterPanel");

        _transitControlPanel.Configure(
            WorkflowPlaceholderPanel.TitleFor(OperatorConsoleScreen.TransitControl),
            WorkflowPlaceholderPanel.Describe(OperatorConsoleScreen.TransitControl));
        _returnControlPanel.Configure(
            WorkflowPlaceholderPanel.TitleFor(OperatorConsoleScreen.ReturnControl),
            WorkflowPlaceholderPanel.Describe(OperatorConsoleScreen.ReturnControl));
        _expeditionRosterPanel.Configure(
            WorkflowPlaceholderPanel.TitleFor(OperatorConsoleScreen.ExpeditionRoster),
            WorkflowPlaceholderPanel.Describe(OperatorConsoleScreen.ExpeditionRoster));

        _diagnosticsButton.Pressed += ToggleDiagnostics;
        _inputSettingsButton.Pressed += ShowInputSettings;
        _inputSettingsPanel.CloseRequested += HideInputSettings;
        _operationsBoard.ScreenRequested += NavigateTo;
        _transitControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _returnControlPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
        _expeditionRosterPanel.BackRequested += () => NavigateTo(OperatorConsoleScreen.OperationsBoard);
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
        _returnControlPanel.Visible = screen == OperatorConsoleScreen.ReturnControl;
        _expeditionRosterPanel.Visible = screen == OperatorConsoleScreen.ExpeditionRoster;

        if (screen == OperatorConsoleScreen.OperationsBoard)
        {
            _operationsBoard.Refresh();
        }

        OperationsBoardReadModel model = _operations.GetReadModel();
        _statusLabel.Text = model.AnnouncementSummary;
        _titleLabel.Text = screen switch
        {
            OperatorConsoleScreen.OperationsBoard => "Operations Board online",
            OperatorConsoleScreen.TransitControl => "Transit Control",
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
            case OperatorConsoleScreen.ReturnControl:
                _returnControlPanel.FocusPrimaryAction();
                break;
            case OperatorConsoleScreen.ExpeditionRoster:
                _expeditionRosterPanel.FocusPrimaryAction();
                break;
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
