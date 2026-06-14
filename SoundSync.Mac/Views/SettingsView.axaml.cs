using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using SoundSync.Mac.Audio;

namespace SoundSync.Mac.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        SliderDefaultDelay.Value = 150;
    }

    private void ToggleLaunchAtLogin_Changed(object? sender, RoutedEventArgs e)
    {
        bool on = ToggleLaunchAtLogin.IsChecked == true;
        // TODO: on macOS 13+ use SMAppService.mainApp.register()/unregister()
        // On older macOS write/delete a LaunchAgent plist in ~/Library/LaunchAgents/
    }

    private void SliderDefaultDelay_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        DefaultDelayLabel.Text = $"{(int)e.NewValue} ms";
        // TODO: AppState.Instance.Settings.DefaultDelayMs = (int)e.NewValue;
    }

    private async void BtnUninstall_Click(object? sender, RoutedEventArgs e)
    {
        // Confirm
        var dialog = new Window
        {
            Title = "Uninstall SoundSync",
            Width = 400,
            Height = 180,
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
                               "You will need admin access. Continue?",
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

        // Run uninstall
        BtnUninstall.IsEnabled = false;
        UninstallProgress.IsVisible = true;

        try
        {
            // 1. Release BlackHole (restores original default output)
            UninstallStatusText.Text = "Restoring audio device…";
            BlackHoleManager.Release();

            // 2. Remove BlackHole driver
            UninstallStatusText.Text = "Removing BlackHole driver (admin required)…";
            await BlackHoleManager.UninstallBlackHoleAsync();

            // 3. Delete app data
            UninstallStatusText.Text = "Removing app data…";
            string dataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SoundSync");
            if (Directory.Exists(dataDir))
                Directory.Delete(dataDir, recursive: true);

            // 4. Done — tell user to trash the app
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
