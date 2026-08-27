using WormholeWorlds.Application.Accessibility;
using WormholeWorlds.Infrastructure.Accessibility;

namespace WormholeWorlds.Tests;

public sealed class AccessibilitySettingsTests
{
    [Fact]
    public void SavesAndLoadsValidatedPreferences()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "accessibility.json");
        AccessibilitySettingsStore store = new(path);
        AccessibilityPreferences expected = AccessibilityPreferences.Default with
        {
            UiScalePercent = 150,
            HighContrast = true,
            TimePressureScale = 0.75m,
        };

        store.Save(expected);
        AccessibilityLoadResult loaded = store.LoadOrDefault();

        Assert.Equal(expected, loaded.Preferences);
        Assert.Null(loaded.RecoveryWarning);
    }

    [Fact]
    public void QuarantinesInvalidPreferencesAndRestoresDefaults()
    {
        using TestDirectory directory = new();
        string path = Path.Combine(directory.Path, "accessibility.json");
        File.WriteAllText(path, "{\"schemaVersion\":99}");

        AccessibilityLoadResult loaded = new AccessibilitySettingsStore(path).LoadOrDefault();

        Assert.Equal(AccessibilityPreferences.Default, loaded.Preferences);
        Assert.NotNull(loaded.RecoveryWarning);
        Assert.NotEmpty(Directory.GetFiles(directory.Path, "accessibility.json.corrupt-*.json"));
    }
}
