using Godot;

namespace WormholeWorlds.Presentation;

public partial class StartupFailurePanel : Control
{
    public void Initialize(string sourceLabel, IReadOnlyList<string> errors)
    {
        GetNode<Label>("Background/SafeArea/Layout/Source").Text = $"Source: {sourceLabel}";
        GetNode<Label>("Background/SafeArea/Layout/Details").Text = string.Join("\n", errors.Select(error => $"• {error}"));
    }
}

