using Godot;
using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Application.Transit;
using WormholeWorlds.Core.Transit;

namespace WormholeWorlds.Presentation;

public partial class TransitControl : PanelContainer
{
    private OptionButton _destinationOptions = null!;
    private Label _phaseLabel = null!;
    private Label _progressLabel = null!;
    private Label _resourcesLabel = null!;
    private Label _feedbackLabel = null!;
    private Button _prepareButton = null!;
    private Button _detectIncomingButton = null!;
    private Button _beginSequenceButton = null!;
    private Button _lockNextButton = null!;
    private Button _stabilizeButton = null!;
    private Button _confirmStableButton = null!;
    private Button _abortButton = null!;
    private Button _completeRecoveryButton = null!;
    private Button _completeClosureButton = null!;
    private Button _completeCooldownButton = null!;
    private Button _backButton = null!;
    private TransitSimulationService? _transit;
    private ISimulationClock? _clock;
    private IReadOnlyList<DestinationOption> _destinations = [];
    private string _feedback = "Select a destination or detect an unscheduled incoming Transit Link.";

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _destinationOptions = GetNode<OptionButton>("Margin/Layout/DestinationRow/DestinationOptions");
        _phaseLabel = GetNode<Label>("Margin/Layout/Phase");
        _progressLabel = GetNode<Label>("Margin/Layout/Progress");
        _resourcesLabel = GetNode<Label>("Margin/Layout/Resources");
        _feedbackLabel = GetNode<Label>("Margin/Layout/Feedback");
        _prepareButton = GetNode<Button>("Margin/Layout/Actions/PrepareButton");
        _detectIncomingButton = GetNode<Button>("Margin/Layout/Actions/DetectIncomingButton");
        _beginSequenceButton = GetNode<Button>("Margin/Layout/Actions/BeginSequenceButton");
        _lockNextButton = GetNode<Button>("Margin/Layout/Actions/LockNextButton");
        _stabilizeButton = GetNode<Button>("Margin/Layout/Actions/StabilizeButton");
        _confirmStableButton = GetNode<Button>("Margin/Layout/Actions/ConfirmStableButton");
        _abortButton = GetNode<Button>("Margin/Layout/Actions/AbortButton");
        _completeRecoveryButton = GetNode<Button>("Margin/Layout/Actions/CompleteRecoveryButton");
        _completeClosureButton = GetNode<Button>("Margin/Layout/Actions/CompleteClosureButton");
        _completeCooldownButton = GetNode<Button>("Margin/Layout/Actions/CompleteCooldownButton");
        _backButton = GetNode<Button>("Margin/Layout/Navigation/BackButton");

        _prepareButton.Pressed += OnPreparePressed;
        _detectIncomingButton.Pressed += () => RunTimed(at => _transit!.DetectIncoming(at));
        _beginSequenceButton.Pressed += () => RunTimed(at => _transit!.BeginSequence(at));
        _lockNextButton.Pressed += OnLockNextPressed;
        _stabilizeButton.Pressed += () => RunTimed(at => _transit!.BeginStabilization(at));
        _confirmStableButton.Pressed += () => RunTimed(at => _transit!.ConfirmStable(at));
        _abortButton.Pressed += () => RunTimed(at => _transit!.Abort(at));
        _completeRecoveryButton.Pressed += () => RunTimed(at => _transit!.CompleteRecovery(at));
        _completeClosureButton.Pressed += () => RunTimed(at => _transit!.CompleteClosure(at));
        _completeCooldownButton.Pressed += () => RunTimed(at => _transit!.CompleteCooldown(at));
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Initialize(TransitSimulationService transit, ISimulationClock clock)
    {
        _transit = transit;
        _clock = clock;
        _destinations = transit.ListDestinations();
        _destinationOptions.Clear();
        foreach (DestinationOption destination in _destinations)
        {
            _destinationOptions.AddItem(
                $"{destination.DisplayName} (power {destination.RequiredPowerUnits}, cooling {destination.RequiredCoolingUnits})");
        }

        if (_destinations.Count > 0)
        {
            _destinationOptions.Select(0);
        }

        Refresh();
    }

    public void FocusPrimaryAction()
    {
        if (_prepareButton.Disabled)
        {
            _backButton.GrabFocus();
            return;
        }

        _prepareButton.GrabFocus();
    }

    public void Refresh()
    {
        if (_transit is null)
        {
            return;
        }

        TransitControlReadModel model = _transit.GetTransitControl();
        _phaseLabel.Text = $"Transit Array: {model.PhaseLabel}";
        _progressLabel.Text = model.ProgressSummary;
        _resourcesLabel.Text =
            $"Power free {model.FreePower}/{model.PowerCapacity} (reserved {model.ReservedPower}) · " +
            $"Cooling free {model.FreeCooling}/{model.CoolingCapacity} (reserved {model.ReservedCooling})";
        _feedbackLabel.Text = _feedback;

        bool selecting = model.CanPrepare;
        _destinationOptions.Disabled = !selecting;
        _prepareButton.Disabled = !selecting || _destinations.Count == 0;
        _detectIncomingButton.Disabled = !model.CanDetectIncoming;
        _beginSequenceButton.Disabled = !model.CanBeginSequence;
        _lockNextButton.Disabled = !model.CanLockVector;
        _stabilizeButton.Disabled = !model.CanStabilize;
        _confirmStableButton.Disabled = !model.CanConfirmStable;
        _abortButton.Disabled = !model.CanAbort;
        _completeRecoveryButton.Disabled = !model.CanCompleteRecovery;
        _completeClosureButton.Disabled = !model.CanCompleteClosure;
        _completeCooldownButton.Disabled = !model.CanCompleteCooldown;

        _lockNextButton.Text = model.NextLockElement is null
            ? "Lock next vector element"
            : $"Lock next ({model.NextLockElement})";
    }

    private void OnPreparePressed()
    {
        if (_transit is null || _clock is null || _destinations.Count == 0)
        {
            return;
        }

        int index = Math.Clamp(_destinationOptions.Selected, 0, _destinations.Count - 1);
        ApplyResult(_transit.PrepareSelected(_destinations[index].Id, AdvanceClock()));
    }

    private void OnLockNextPressed()
    {
        if (_transit is null)
        {
            return;
        }

        ApplyResult(_transit.LockNextExpected());
    }

    private void RunTimed(Func<SimulationInstant, OutgoingOperationResult> command)
    {
        if (_transit is null || _clock is null)
        {
            return;
        }

        ApplyResult(command(AdvanceClock()));
    }

    private SimulationInstant AdvanceClock()
    {
        if (_clock is null)
        {
            return SimulationInstant.Zero;
        }

        return _clock.Advance(1000);
    }

    private void ApplyResult(OutgoingOperationResult result)
    {
        _feedback = result.IsAccepted
            ? $"Accepted. Transit Array is now {FormatPhase(result.Snapshot.TransitArray.Phase)}."
            : $"{result.Rejection!.ReasonCode}: {result.Rejection.CorrectiveAction}";
        Refresh();
        StateChanged?.Invoke();
    }

    private static string FormatPhase(TransitArrayPhase phase) => phase switch
    {
        TransitArrayPhase.Standby => "Standby",
        TransitArrayPhase.OutgoingPreparation => "Outgoing preparation",
        TransitArrayPhase.IncomingDetected => "Incoming detected",
        TransitArrayPhase.Sequencing => "Sequencing",
        TransitArrayPhase.Stabilizing => "Stabilizing",
        TransitArrayPhase.LinkOpen => "Link open",
        TransitArrayPhase.Closing => "Closing",
        TransitArrayPhase.Cooldown => "Cooldown",
        TransitArrayPhase.Recovering => "Recovering",
        TransitArrayPhase.Faulted => "Faulted",
        _ => phase.ToString(),
    };
}
