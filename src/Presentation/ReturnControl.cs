using Godot;
using WormholeWorlds.Application.Security;
using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Security;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Presentation;

public partial class ReturnControl : PanelContainer
{
    private OptionButton _scenarioOptions = null!;
    private Label _warningLabel = null!;
    private Label _linkLabel = null!;
    private Label _credentialLabel = null!;
    private Label _shutterLabel = null!;
    private Label _feedbackLabel = null!;
    private Button _verifyButton = null!;
    private Button _openButton = null!;
    private Button _confirmOpenedButton = null!;
    private Button _closeButton = null!;
    private Button _confirmClosedButton = null!;
    private Button _reportFaultButton = null!;
    private Button _resetFaultButton = null!;
    private Button _backButton = null!;
    private ReturnSecurityService? _security;
    private TransitSimulationService? _transit;
    private ISimulationClock? _clock;
    private IReadOnlyList<ReturnCredentialScenario> _scenarios = [];
    private string _feedback = "Select a credential case and verify before changing containment.";

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _scenarioOptions = GetNode<OptionButton>("Margin/Layout/ScenarioRow/ScenarioOptions");
        _warningLabel = GetNode<Label>("Margin/Layout/Warning");
        _linkLabel = GetNode<Label>("Margin/Layout/Link");
        _credentialLabel = GetNode<Label>("Margin/Layout/Credential");
        _shutterLabel = GetNode<Label>("Margin/Layout/Shutter");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _verifyButton = GetNode<Button>("Margin/Layout/Actions/VerifyButton");
        _openButton = GetNode<Button>("Margin/Layout/Actions/OpenButton");
        _confirmOpenedButton = GetNode<Button>("Margin/Layout/Actions/ConfirmOpenedButton");
        _closeButton = GetNode<Button>("Margin/Layout/Actions/CloseButton");
        _confirmClosedButton = GetNode<Button>("Margin/Layout/Actions/ConfirmClosedButton");
        _reportFaultButton = GetNode<Button>("Margin/Layout/Actions/ReportFaultButton");
        _resetFaultButton = GetNode<Button>("Margin/Layout/Actions/ResetFaultButton");
        _backButton = GetNode<Button>("Margin/Layout/Navigation/BackButton");

        _verifyButton.Pressed += OnVerifyPressed;
        _openButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.Open);
        _confirmOpenedButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.ConfirmOpened);
        _closeButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.Close);
        _confirmClosedButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.ConfirmClosed);
        _reportFaultButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.ReportFault, "operator_reported_fault");
        _resetFaultButton.Pressed += () => RunShutter(ContainmentShutterCommandKind.ResetFault);
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Initialize(
        ReturnSecurityService security,
        TransitSimulationService transit,
        ISimulationClock clock)
    {
        _security = security;
        _transit = transit;
        _clock = clock;
        _scenarios = security.ListScenarios();
        _scenarioOptions.Clear();
        foreach (ReturnCredentialScenario scenario in _scenarios)
        {
            _scenarioOptions.AddItem($"{scenario.DisplayName} — {scenario.WarningHint}");
        }

        if (_scenarios.Count > 0)
        {
            _scenarioOptions.Select(0);
        }

        Refresh();
    }

    public void FocusPrimaryAction()
    {
        _verifyButton.GrabFocus();
    }

    public void Refresh()
    {
        if (_security is null || _transit is null)
        {
            return;
        }

        ReturnSecurityReadModel model = _security.GetReadModel(_transit.GetTransitArraySnapshot());
        _warningLabel.Text = $"Warning: {model.WarningCategory} — {model.WarningSummary}";
        _linkLabel.Text = model.LinkIsStable
            ? "Transit Link: stable (Link open)"
            : "Transit Link: not stable. Establish a stable link in Transit Control before opening containment.";
        _credentialLabel.Text = model.CredentialStatus is null
            ? $"Credential: unchecked ({model.StatusCode})"
            : $"Credential: {model.CredentialStatus} / {model.AuthorizationOutcome} ({model.StatusCode})";
        _shutterLabel.Text = $"Containment Shutter: {model.ShutterStateLabel}";
        _feedbackLabel.Text = _feedback;

        _verifyButton.Disabled = false;
        _openButton.Disabled = !model.CanOpenShutter;
        _confirmOpenedButton.Disabled = !model.CanConfirmOpened;
        _closeButton.Disabled = !model.CanCloseShutter;
        _confirmClosedButton.Disabled = !model.CanConfirmClosed;
        _reportFaultButton.Disabled = !model.CanReportFault;
        _resetFaultButton.Disabled = !model.CanResetFault;
    }

    private void OnVerifyPressed()
    {
        if (_security is null || _clock is null || _scenarios.Count == 0)
        {
            return;
        }

        int index = Math.Clamp(_scenarioOptions.Selected, 0, _scenarios.Count - 1);
        CredentialVerificationResult result = _security.VerifyScenario(_scenarios[index].Id, AdvanceClock());
        _feedback = $"{result.Assessment.Outcome}: {result.Assessment.ReasonCode} — {result.Assessment.CorrectiveAction}";
        Refresh();
        StateChanged?.Invoke();
    }

    private void RunShutter(ContainmentShutterCommandKind kind, string? reasonCode = null)
    {
        if (_security is null || _transit is null || _clock is null)
        {
            return;
        }

        ContainmentShutterResult result = _security.ExecuteShutter(
            kind,
            AdvanceClock(),
            _transit.GetTransitArraySnapshot(),
            reasonCode);

        _feedback = result.IsAccepted
            ? $"Accepted. Containment Shutter is now {result.Snapshot.State}."
            : $"{result.Rejection!.ReasonCode}: {result.Rejection.CorrectiveAction}";
        Refresh();
        StateChanged?.Invoke();
    }

    private SimulationInstant AdvanceClock()
    {
        if (_clock is null)
        {
            return SimulationInstant.Zero;
        }

        return _clock.Advance(1000);
    }
}
