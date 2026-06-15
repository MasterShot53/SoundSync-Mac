using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using SoundSync.Mac.Audio;
using SoundSync.Mac.Controls;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class SettingsView : UserControl
{
    private bool _loading;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadFromAppState();
    }

    private void LoadFromAppState()
    {
        _loading = true;
        ToggleAutoStart.IsChecked     = AppState.Instance.AutoStart;
        ToggleAutoCalibrate.IsChecked = AppState.Instance.AutoCalibrate;
        SliderDriftInterval.Value     = AppState.Instance.DriftCorrectionIntervalMs / 1000.0;
        SliderDefaultDelay.Value      = AppState.Instance.DefaultDelayMs;
        _loading = false;
        UpdateDriftLabel((int)SliderDriftInterval.Value);
    }

    // ── Auto Start ───────────────────────────────────────────────────────────

    private void ToggleAutoStart_Changed(object? sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppState.Instance.AutoStart = ToggleAutoStart.IsChecked == true;
    }

    // ── Auto Calibrate ───────────────────────────────────────────────────────

    private void ToggleAutoCalibrate_Changed(object? sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppState.Instance.AutoCalibrate = ToggleAutoCalibrate.IsChecked == true;
    }

    // ── Drift Interval ───────────────────────────────────────────────────────

    private void SliderDriftInterval_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_loading) return;
        int secs = (int)e.NewValue;
        AppState.Instance.DriftCorrectionIntervalMs = secs * 1000;
        UpdateDriftLabel(secs);
    }

    private void UpdateDriftLabel(int secs)
    {
        DriftIntervalLabel.Text = secs >= 60
            ? $"{secs / 60} min {secs % 60:D2} s"
            : $"{secs} s";
    }

    // ── Launch at Login ──────────────────────────────────────────────────────

    private void ToggleLaunchAtLogin_Changed(object? sender, RoutedEventArgs e)
    {
        if (_loading) return;
        // TODO: SMAppService.mainApp.register() / unregister() (macOS 13+)
        // or write/delete ~/Library/LaunchAgents/com.soundsync.plist
    }

    // ── Default Delay ────────────────────────────────────────────────────────

    private void SliderDefaultDelay_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (_loading) return;
        AppState.Instance.DefaultDelayMs = (int)e.NewValue;
        DefaultDelayLabel.Text = $"{(int)e.NewValue} ms";
    }

    // ── Uninstall ────────────────────────────────────────────────────────────

    private async void BtnUninstall_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new Window
        {
            Title = "Uninstall SoundSync",
            Width = 400, Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(24),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = "This will remove the BlackHole audio driver and all SoundSync data. " +
                               "Admin access is required. Continue?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        FontSize = 13
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        Spacing = 10,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Children =
                        {
                            new Button { Content = "Cancel",    Tag = false },
                            new Button { Content = "Uninstall", Tag = true,
                                Foreground = new Avalonia.Media.SolidColorBrush(
                                    Avalonia.Media.Color.Parse("#FF4444")) }
                        }
                    }
                }
            }
        };

        bool confirmed = false;
        foreach (var btn in ((StackPanel)((StackPanel)dialog.Content!).Children[1]).Children.OfType<Button>())
            btn.Click += (_, _) => { confirmed = btn.Tag is true; dialog.Close(); };

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner != null) await dialog.ShowDialog(owner);
        if (!confirmed) return;

        BtnUninstall.IsEnabled = false;
        UninstallProgress.IsVisible = true;

        try
        {
            UninstallStatusText.Text = "Restoring audio device…";
            BlackHoleManager.Release();

            UninstallStatusText.Text = "Removing BlackHole driver (admin required)…";
            await BlackHoleManager.UninstallBlackHoleAsync();

            UninstallStatusText.Text = "Removing app data…";
            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SoundSync");
            if (Directory.Exists(dataDir))
                Directory.Delete(dataDir, recursive: true);

            UninstallStatusText.Text = "Done. Drag SoundSync.app to Trash to finish.";
            UninstallProgressBar.IsIndeterminate = false;
            UninstallProgressBar.Value = 100;
        }
        catch (Exception ex)
        {
            UninstallStatusText.Text = $"Uninstall failed: {ex.Message}";
            UninstallProgressBar.IsIndeterminate = false;
            BtnUninstall.IsEnabled = true;
        }
    }
}
