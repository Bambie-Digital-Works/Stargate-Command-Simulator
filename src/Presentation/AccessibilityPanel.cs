using Godot;
using WormholeWorlds.Application.Accessibility;
using WormholeWorlds.Application.Simulation;
using WormholeWorlds.Infrastructure.Accessibility;

namespace WormholeWorlds.Presentation;

public partial class AccessibilityPanel : PanelContainer
{
    private OptionButton _scale = null!;
    private CheckButton _highContrast = null!;
    private CheckButton _reducedFlashing = null!;
    private CheckButton _captions = null!;
    private HSlider _masterVolume = null!;
    private HSlider _uiVolume = null!;
    private HSlider _alarmVolume = null!;
    private OptionButton _timePressure = null!;
    private Label _status = null!;
    private AccessibilitySettingsStore? _store;
    private ISimulationClock? _clock;

    public event Action? CloseRequested;
    public event Action<AccessibilityPreferences>? PreferencesChanged;

    public override void _Ready()
    {
        _scale = GetNode<OptionButton>("Margin/Layout/Settings/ScaleValue");
        _highContrast = GetNode<CheckButton>("Margin/Layout/Settings/HighContrastValue");
        _reducedFlashing = GetNode<CheckButton>("Margin/Layout/Settings/ReducedFlashingValue");
        _captions = GetNode<CheckButton>("Margin/Layout/Settings/CaptionsValue");
        _masterVolume = GetNode<HSlider>("Margin/Layout/Settings/MasterVolumeValue");
        _uiVolume = GetNode<HSlider>("Margin/Layout/Settings/UiVolumeValue");
        _alarmVolume = GetNode<HSlider>("Margin/Layout/Settings/AlarmVolumeValue");
        _timePressure = GetNode<OptionButton>("Margin/Layout/Settings/TimePressureValue");
        _status = GetNode<Label>("Margin/Layout/Status");

        foreach (string label in new[] { "100%", "125%", "150%", "200%" })
        {
            _scale.AddItem(label);
        }

        foreach (string label in new[] { "Relaxed (75%)", "Standard (100%)", "Urgent (125%)" })
        {
            _timePressure.AddItem(label);
        }

        GetNode<Button>("Margin/Layout/Footer/DefaultsButton").Pressed += RestoreDefaults;
        GetNode<Button>("Margin/Layout/Footer/ApplyButton").Pressed += SaveAndApply;
        GetNode<Button>("Margin/Layout/Footer/CloseButton").Pressed += () => CloseRequested?.Invoke();
    }

    public void Initialize(
        AccessibilitySettingsStore store,
        ISimulationClock clock,
        AccessibilityPreferences preferences,
        string? recoveryWarning)
    {
        _store = store;
        _clock = clock;
        SetControls(preferences);
        _status.Text = recoveryWarning ?? "Accessibility preferences are stored locally.";
    }

    public void FocusPrimaryAction() => _scale.GrabFocus();

    private void SaveAndApply()
    {
        if (_store is null || _clock is null)
        {
            return;
        }

        AccessibilityPreferences preferences = ReadControls();
        _store.Save(preferences);
        _clock.SetTimeScale(preferences.TimePressureScale);
        _status.Text = "Accessibility preferences saved and applied.";
        PreferencesChanged?.Invoke(preferences);
    }

    private void RestoreDefaults()
    {
        SetControls(AccessibilityPreferences.Default);
        SaveAndApply();
        _status.Text = "Accessibility defaults restored.";
    }

    private AccessibilityPreferences ReadControls() => new(
        1,
        new[] { 100, 125, 150, 200 }[_scale.Selected],
        _highContrast.ButtonPressed,
        _reducedFlashing.ButtonPressed,
        _captions.ButtonPressed,
        (int)_masterVolume.Value,
        (int)_uiVolume.Value,
        (int)_alarmVolume.Value,
        new[] { 0.75m, 1m, 1.25m }[_timePressure.Selected]);

    private void SetControls(AccessibilityPreferences preferences)
    {
        _scale.Select(Array.IndexOf(new[] { 100, 125, 150, 200 }, preferences.UiScalePercent));
        _highContrast.ButtonPressed = preferences.HighContrast;
        _reducedFlashing.ButtonPressed = preferences.ReducedFlashing;
        _captions.ButtonPressed = preferences.CaptionsEnabled;
        _masterVolume.Value = preferences.MasterVolumePercent;
        _uiVolume.Value = preferences.UiVolumePercent;
        _alarmVolume.Value = preferences.AlarmVolumePercent;
        _timePressure.Select(Array.IndexOf(new[] { 0.75m, 1m, 1.25m }, preferences.TimePressureScale));
    }
}
