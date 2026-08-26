using FacilityCommand.Application.Simulation;
using FacilityCommand.Application.Survey;
using FacilityCommand.Core.Survey;
using Godot;

namespace FacilityCommand.Presentation;

public partial class SurveyTelemetry : PanelContainer
{
    private Label _stateLabel = null!;
    private Label _destinationLabel = null!;
    private Label _statusLabel = null!;
    private Label _riskSummaryLabel = null!;
    private Label _suggestedRiskLabel = null!;
    private Label _recordedDecisionLabel = null!;
    private Label _channelsLabel = null!;
    private Label _feedbackLabel = null!;
    private Button _deployButton = null!;
    private Button _resolveButton = null!;
    private Button _acceptableButton = null!;
    private Button _elevatedButton = null!;
    private Button _unacceptableButton = null!;
    private Button _backButton = null!;
    private SurveyTelemetryService? _survey;
    private ISimulationClock? _clock;
    private string _feedback = "Open a stable outgoing link and equip survey_kit before deploying.";

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _stateLabel = GetNode<Label>("Margin/Layout/State");
        _destinationLabel = GetNode<Label>("Margin/Layout/Destination");
        _statusLabel = GetNode<Label>("Margin/Layout/Status");
        _riskSummaryLabel = GetNode<Label>("Margin/Layout/RiskSummary");
        _suggestedRiskLabel = GetNode<Label>("Margin/Layout/SuggestedRisk");
        _recordedDecisionLabel = GetNode<Label>("Margin/Layout/RecordedDecision");
        _channelsLabel = GetNode<Label>("Margin/Layout/Channels");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _deployButton = GetNode<Button>("Margin/Layout/Actions/DeployButton");
        _resolveButton = GetNode<Button>("Margin/Layout/Actions/ResolveButton");
        _acceptableButton = GetNode<Button>("Margin/Layout/Decisions/AcceptableButton");
        _elevatedButton = GetNode<Button>("Margin/Layout/Decisions/ElevatedButton");
        _unacceptableButton = GetNode<Button>("Margin/Layout/Decisions/UnacceptableButton");
        _backButton = GetNode<Button>("Margin/Layout/Navigation/BackButton");

        _deployButton.Pressed += OnDeployPressed;
        _resolveButton.Pressed += OnResolvePressed;
        _acceptableButton.Pressed += () => RecordDecision(SurveyRiskAssessment.Acceptable);
        _elevatedButton.Pressed += () => RecordDecision(SurveyRiskAssessment.Elevated);
        _unacceptableButton.Pressed += () => RecordDecision(SurveyRiskAssessment.Unacceptable);
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Initialize(SurveyTelemetryService survey, ISimulationClock clock)
    {
        _survey = survey;
        _clock = clock;
        Refresh();
    }

    public void FocusPrimaryAction()
    {
        if (!_deployButton.Disabled)
        {
            _deployButton.GrabFocus();
            return;
        }

        if (!_resolveButton.Disabled)
        {
            _resolveButton.GrabFocus();
            return;
        }

        if (!_acceptableButton.Disabled)
        {
            _acceptableButton.GrabFocus();
            return;
        }

        _backButton.GrabFocus();
    }

    public void Refresh()
    {
        if (_survey is null)
        {
            return;
        }

        SurveyTelemetryReadModel model = _survey.GetReadModel();
        _stateLabel.Text = $"Survey Drone: {model.DeployStateLabel}";
        _destinationLabel.Text = string.IsNullOrWhiteSpace(model.DestinationId)
            ? "Destination: none"
            : $"Destination: {model.DestinationId}";
        _statusLabel.Text = model.StatusSummary;
        _riskSummaryLabel.Text = model.RiskSummary;
        _suggestedRiskLabel.Text = $"Suggested risk: {model.SuggestedRiskLabel}";
        _recordedDecisionLabel.Text = $"Recorded decision: {model.RecordedDecisionLabel}";
        _channelsLabel.Text = model.Channels.Count == 0
            ? "Channels\nNo telemetry yet."
            : "Channels\n" + string.Join(
                '\n',
                model.Channels.Select(channel =>
                {
                    string line = $"[{channel.QualityLabel}] {channel.ChannelLabel}: {channel.ReportedValue}";
                    return string.IsNullOrWhiteSpace(channel.ContradictionNote)
                        ? line
                        : $"{line} — {channel.ContradictionNote}";
                }));
        _feedbackLabel.Text = _feedback;
        _deployButton.Disabled = !model.CanDeploy;
        _resolveButton.Disabled = !model.CanResolveTelemetry;
        _acceptableButton.Disabled = !model.CanRecordDecision;
        _elevatedButton.Disabled = !model.CanRecordDecision;
        _unacceptableButton.Disabled = !model.CanRecordDecision;
    }

    private void OnDeployPressed()
    {
        if (_survey is null || _clock is null)
        {
            return;
        }

        _clock.Advance(1000);
        ApplyResult(_survey.Deploy());
    }

    private void OnResolvePressed()
    {
        if (_survey is null || _clock is null)
        {
            return;
        }

        _clock.Advance(1000);
        ApplyResult(_survey.ResolvePendingTelemetry());
    }

    private void RecordDecision(SurveyRiskAssessment decision)
    {
        if (_survey is null || _clock is null)
        {
            return;
        }

        _clock.Advance(1000);
        ApplyResult(_survey.RecordRiskDecision(decision));
    }

    private void ApplyResult(SurveyOperationResult result)
    {
        _feedback = result.IsAccepted
            ? "Accepted."
            : $"{result.Rejection!.ReasonCode}: {result.Rejection.CorrectiveAction}";
        Refresh();
        StateChanged?.Invoke();
    }
}
