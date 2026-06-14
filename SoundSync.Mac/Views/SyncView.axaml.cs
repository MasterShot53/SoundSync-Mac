using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SoundSync.Mac.Audio;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class SyncView : UserControl
{
    // Set by MainWindow — single shared engine instance
    public CoreAudioEngine? Engine { get; set; }

    public SyncView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            RefreshBlackHoleBanner();
            Refresh();
            UpdateUI();
        };

        AppState.Instance.Devices.CollectionChanged += (_, _) => Refresh();

        AppState.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppState.EngineRunning)
                                or nameof(AppState.IsCalibrating)
                                or nameof(AppState.IsBeatPlaying)
                                or nameof(AppState.DriftCorrection)
                                or nameof(AppState.AutoCalibrate))
                UpdateUI();

            if (e.PropertyName == nameof(AppState.EngineRunning))
                Refresh();
        };
    }

    // ── Public API called by MainWindow ─────────────────────────────────────

    public void Refresh()
    {
        var devices = AppState.Instance.Devices;
        SyncEmpty.IsVisible = devices.Count == 0;

        while (SyncCards.Children.Count > 1)
            SyncCards.Children.RemoveAt(SyncCards.Children.Count - 1);

        foreach (var dev in devices)
            SyncCards.Children.Add(BuildSyncCard(dev));
    }

    public void UpdateUI()
    {
        bool running    = AppState.Instance.EngineRunning;
        bool calibrating = AppState.Instance.IsCalibrating;

        EngineStatusText.Text = AppState.Instance.EngineStatus;

        BtnBeat.IsEnabled          = running;
        BtnAutoCalibrate.IsEnabled = running;
        BtnCalibrate.IsEnabled     = running && !calibrating;
        BtnSyncNow.IsEnabled       = running;
        BtnDriftCorrection.IsEnabled = running;

        BtnBeat.Content   = AppState.Instance.IsBeatPlaying ? "Stop Beat" : "Play Beat";
        ((ToggleButton)BtnAutoCalibrate).IsChecked = AppState.Instance.AutoCalibrate;
        BtnAutoCalibrate.Content = AppState.Instance.AutoCalibrate ? "Auto: On" : "Auto: Off";
        ((ToggleButton)BtnDriftCorrection).IsChecked = AppState.Instance.DriftCorrection;
        BtnDriftCorrection.Content = AppState.Instance.DriftCorrection ? "On" : "Off";
    }

    // ── Per-device delay card ────────────────────────────────────────────────

    private Control BuildSyncCard(SpeakerDevice dev)
    {
        var card = new Border { Padding = new Avalonia.Thickness(16, 13) };
        card.Classes.Add("Card");

        var stack = new StackPanel { Spacing = 10 };

        // Header: name | delay badge | reset
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var name = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        name.Bind(TextBlock.TextProperty, new Binding(nameof(dev.Name)) { Source = dev });

        var badge = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#252A35")),
            CornerRadius = new Avalonia.CornerRadius(8),
            Padding = new Avalonia.Thickness(10, 4, 10, 4),
            Margin = new Avalonia.Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var badgeText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#2266FF"))
        };
        badgeText.Bind(TextBlock.TextProperty,
            new Binding(nameof(dev.DelayDisplayText)) { Source = dev });
        badge.Child = badgeText;

        var resetBtn = new Button
        {
            Content = "Reset",
            FontSize = 12,
            Padding = new Avalonia.Thickness(8, 4, 8, 4)
        };
        resetBtn.Classes.Add("Secondary");
        resetBtn.Click += (_, _) => dev.DelayOffsetMs = 0;

        Grid.SetColumn(name, 0);
        Grid.SetColumn(badge, 1);
        Grid.SetColumn(resetBtn, 2);
        header.Children.Add(name);
        header.Children.Add(badge);
        header.Children.Add(resetBtn);

        // Delay slider with range labels
        var sliderGrid = new Grid { Margin = new Avalonia.Thickness(0, 4, 0, 0) };
        sliderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        sliderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        sliderGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        var minLbl = new TextBlock
        {
            Text = "-600 ms", FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#4B4F6B")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        var slider = new Slider
        {
            Minimum = -600, Maximum = 600,
            TickFrequency = 10, SmallChange = 10, LargeChange = 50,
            VerticalAlignment = VerticalAlignment.Center
        };
        // Sync slider ↔ dev.DelayOffsetMs manually to avoid float/double converter issues
        slider.Value = dev.DelayOffsetMs;
        slider.ValueChanged += (_, args) => dev.DelayOffsetMs = (float)args.NewValue;
        dev.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(dev.DelayOffsetMs) &&
                Math.Abs(slider.Value - dev.DelayOffsetMs) > 0.5)
                slider.Value = dev.DelayOffsetMs;
        };

        var maxLbl = new TextBlock
        {
            Text = "+600 ms", FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#4B4F6B")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(8, 0, 0, 0)
        };

        Grid.SetColumn(minLbl, 0);
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(maxLbl, 2);
        sliderGrid.Children.Add(minLbl);
        sliderGrid.Children.Add(slider);
        sliderGrid.Children.Add(maxLbl);

        stack.Children.Add(header);
        stack.Children.Add(sliderGrid);
        card.Child = stack;
        return card;
    }

    // ── Event handlers ───────────────────────────────────────────────────────

    private void RefreshBlackHoleBanner()
    {
        BlackHoleBanner.IsVisible = !BlackHoleManager.IsInstalled();
    }

    private async void BtnGetBlackHole_Click(object? sender, RoutedEventArgs e)
    {
        BtnGetBlackHole.IsEnabled = false;
        BtnGetBlackHole.Content   = "Installing…";
        EngineStatusText.Text     = "Downloading BlackHole audio driver…";
        try
        {
            var progress = new Progress<string>(msg => EngineStatusText.Text = msg);
            await BlackHoleManager.InstallAsync(progress);
            RefreshBlackHoleBanner();
            EngineStatusText.Text = "BlackHole installed. You can now start the engine.";
        }
        catch (Exception ex)
        {
            EngineStatusText.Text     = $"Install failed: {ex.Message}";
            BtnGetBlackHole.IsEnabled = true;
            BtnGetBlackHole.Content   = "Retry Install";
        }
    }

    private void BtnBeat_Click(object? sender, RoutedEventArgs e)
    {
        AppState.Instance.IsBeatPlaying = !AppState.Instance.IsBeatPlaying;
        if (AppState.Instance.IsBeatPlaying) Engine?.StartBeat(); else Engine?.StopBeat();
        UpdateUI();
    }

    private void BtnAutoCalibrate_Click(object? sender, RoutedEventArgs e)
    {
        AppState.Instance.AutoCalibrate = ((ToggleButton)sender!).IsChecked == true;
        if (AppState.Instance.AutoCalibrate) Engine?.StartAutoCalibrate();
        else Engine?.StopAutoCalibrate();
        UpdateUI();
    }

    private async void BtnCalibrate_Click(object? sender, RoutedEventArgs e)
    {
        AppState.Instance.IsCalibrating = true;
        BtnCalibrate.Content = "Calibrating…";
        UpdateUI();
        await (Engine?.CalibrateAsync(AppState.Instance.Devices) ?? Task.CompletedTask);
        AppState.Instance.IsCalibrating = false;
        BtnCalibrate.Content = "Calibrate Now";
        UpdateUI();
    }

    private void BtnSyncNow_Click(object? sender, RoutedEventArgs e) => Engine?.SyncNow();

    private void BtnDriftCorrection_Click(object? sender, RoutedEventArgs e)
    {
        AppState.Instance.DriftCorrection = !AppState.Instance.DriftCorrection;
        if (AppState.Instance.DriftCorrection) Engine?.StartDriftCorrection();
        else Engine?.StopDriftCorrection();
        UpdateUI();
    }
}
