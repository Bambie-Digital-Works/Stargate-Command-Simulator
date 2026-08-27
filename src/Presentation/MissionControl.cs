using Godot;
using WormholeWorlds.Application.Missions;
using WormholeWorlds.Application.Operations;
using WormholeWorlds.Core.Missions;

namespace WormholeWorlds.Presentation;

public partial class MissionControl : PanelContainer
{
    private OptionButton _missionSelector = null!;
    private Label _summaryLabel = null!;
    private Label _objectiveLabel = null!;
    private Label _progressLabel = null!;
    private Label _statusLabel = null!;
    private Button _startButton = null!;
    private Button _advanceButton = null!;
    private Button _returnButton = null!;
    private Button _abortButton = null!;
    private Button _backButton = null!;
    private OperationsBoardService? _operations;
    private readonly List<string> _missionIds = [];

    public event Action? BackRequested;
    public event Action? StateChanged;

    public override void _Ready()
    {
        _missionSelector = GetNode<OptionButton>("Margin/Layout/MissionSelector");
        _summaryLabel = GetNode<Label>("Margin/Layout/Summary");
        _objectiveLabel = GetNode<Label>("Margin/Layout/Objective");
        _progressLabel = GetNode<Label>("Margin/Layout/Progress");
        _statusLabel = GetNode<Label>("Margin/Layout/Status");
        _startButton = GetNode<Button>("Margin/Layout/Actions/StartButton");
        _advanceButton = GetNode<Button>("Margin/Layout/Actions/AdvanceButton");
        _returnButton = GetNode<Button>("Margin/Layout/Actions/ReturnButton");
        _abortButton = GetNode<Button>("Margin/Layout/Actions/AbortButton");
        _backButton = GetNode<Button>("Margin/Layout/Actions/BackButton");
        _missionSelector.ItemSelected += _ => Refresh();
        _startButton.Pressed += StartMission;
        _advanceButton.Pressed += AdvanceMission;
        _returnButton.Pressed += CompleteReturn;
        _abortButton.Pressed += AbortMission;
        _backButton.Pressed += () => BackRequested?.Invoke();
    }

    public void Initialize(OperationsBoardService operations)
    {
        _operations = operations;
        Refresh();
    }

    public void Refresh()
    {
        if (_operations is null)
        {
            return;
        }

        MissionReadModel model = _operations.Missions.GetReadModel();
        string selectedId = _missionSelector.Selected >= 0 && _missionSelector.Selected < _missionIds.Count
            ? _missionIds[_missionSelector.Selected]
            : model.AvailableMissions.FirstOrDefault()?.Id ?? string.Empty;
        _missionSelector.Clear();
        _missionIds.Clear();
        foreach (MissionDefinition mission in model.AvailableMissions)
        {
            _missionIds.Add(mission.Id);
            _missionSelector.AddItem($"{mission.DisplayName} — risk {mission.RiskRating}/5");
        }

        int selectedIndex = _missionIds.IndexOf(selectedId);
        if (selectedIndex >= 0)
        {
            _missionSelector.Select(selectedIndex);
        }

        _summaryLabel.Text = $"{model.ActiveMissionLabel} [{model.PhaseLabel}]";
        _objectiveLabel.Text = $"{model.DestinationId}\n{model.Objective}";
        _progressLabel.Text = $"Field progress: {model.ProgressPercent}%";
        _statusLabel.Text = model.StatusSummary;
        bool active = model.Phase is MissionPhase.InField or MissionPhase.AwaitingReturn;
        _startButton.Disabled = active || _missionSelector.Selected < 0;
        _advanceButton.Disabled = model.Phase != MissionPhase.InField;
        _returnButton.Disabled = model.Phase != MissionPhase.AwaitingReturn;
        _abortButton.Disabled = !active;
    }

    public void FocusPrimaryAction() => (_startButton.Disabled ? _backButton : _startButton).GrabFocus();

    private void StartMission()
    {
        if (_operations is null || _missionSelector.Selected < 0)
        {
            return;
        }

        _operations.StartMission(_missionIds[_missionSelector.Selected]);
        Refresh();
        StateChanged?.Invoke();
    }

    private void AdvanceMission()
    {
        if (_operations is null)
        {
            return;
        }

        _operations.AdvanceMission(30000);
        Refresh();
        StateChanged?.Invoke();
    }

    private void CompleteReturn()
    {
        if (_operations is null)
        {
            return;
        }

        _operations.CompleteMissionReturn(true, "Mission returned with its objective report.");
        Refresh();
        StateChanged?.Invoke();
    }

    private void AbortMission()
    {
        if (_operations is null)
        {
            return;
        }

        _operations.AbortMission("Operator ordered an emergency extraction.");
        Refresh();
        StateChanged?.Invoke();
    }
}
