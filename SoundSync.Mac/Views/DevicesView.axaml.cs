using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SoundSync.Audio;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class DevicesView : UserControl
{
    private SpeakerDevice? _detailDevice;
    private readonly HashSet<string> _expandedCards = new();
    private bool _driftLoading;

    public event Action? CalibrateRequested;
    public event Action? BeatToggleRequested;
    public event Action? SyncNowRequested;

    public DevicesView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            RebuildDeviceList();
            UpdateStatusCards();
            UpdateSyncCardLastCalibration();
            _driftLoading = true;
            DriftToggle.IsChecked = AppState.Instance.DriftCorrection;
            _driftLoading = false;
        };

        AppState.Instance.Devices.CollectionChanged += (_, _) =>
        {
            RebuildDeviceList();
            UpdateStatusCards();
        };

        AppState.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(AppState.EngineRunning)
                               or nameof(AppState.AudioSourceName)
                               or nameof(AppState.AudioSourceRate))
                UpdateStatusCards();

            if (e.PropertyName == nameof(AppState.IsBeatPlaying))
                BeatLabel.Text = AppState.Instance.IsBeatPlaying ? "Stop Beat" : "Play Beat";

            if (e.PropertyName == nameof(AppState.LastCalibrationTime))
                UpdateSyncCardLastCalibration();
        };
    }

    // ── Public update methods ─────────────────────────────────────────────

    public void UpdateStatusCards()
    {
        bool running = AppState.Instance.EngineRunning;

        if (running)
        {
            CardEngineDot.Fill = new SolidColorBrush(Color.Parse("#3DDC84"));
            CardEngineText.Text = "Running";
            CardEngineText.Foreground = new SolidColorBrush(Color.Parse("#3DDC84"));
        }
        else
        {
            CardEngineDot.Fill = new SolidColorBrush(Color.Parse("#4B4F6B"));
            CardEngineText.Text = "Stopped";
            CardEngineText.Foreground = new SolidColorBrush(Color.Parse("#8B8FA8"));
        }

        CardSourceName.Text = AppState.Instance.AudioSourceName;
        CardSourceRate.Text = AppState.Instance.AudioRateText;

        int cnt = AppState.Instance.Devices.Count;
        CardDeviceCount.Text = cnt.ToString();

        SyncHealthLabel.Text = "Excellent";
        SyncHealthLabel.Foreground = new SolidColorBrush(Color.Parse("#3DDC84"));
        CardSyncHealth.Text = "Excellent";
        CardSyncHealth.Foreground = new SolidColorBrush(Color.Parse("#3DDC84"));
    }

    public void UpdateSyncCardLastCalibration()
    {
        var t = AppState.Instance.LastCalibrationTime;
        LastCalibrationText.Text = t.HasValue ? t.Value.ToString("h:mm tt") : "Never";
    }

    // ── Device list ───────────────────────────────────────────────────────

    private void RebuildDeviceList()
    {
        DeviceList.Children.Clear();
        var devices = AppState.Instance.Devices;
        EmptyState.IsVisible = devices.Count == 0;

        int count = devices.Count;
        SubtitleText.Text = count == 0 ? "Manage your connected speakers"
            : count == 1 ? "1 speaker connected"
            : $"{count} speakers connected";

        foreach (var dev in devices)
            DeviceList.Children.Add(BuildActiveCard(dev));
    }

    private Border BuildActiveCard(SpeakerDevice dev)
    {
        bool expanded = _expandedCards.Contains(dev.Id);

        var card = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(14),
            BorderThickness = new Avalonia.Thickness(1),
            Padding = new Avalonia.Thickness(18, 16)
        };
        var cardGrad = new LinearGradientBrush { StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative) };
        cardGrad.GradientStops.Add(new GradientStop(Color.Parse("#1E2230"), 0));
        cardGrad.GradientStops.Add(new GradientStop(Color.Parse("#141820"), 1));
        card.Background = cardGrad;

        var borderGrad = new LinearGradientBrush { StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative) };
        borderGrad.GradientStops.Add(new GradientStop(Color.Parse("#28FFFFFF"), 0));
        borderGrad.GradientStops.Add(new GradientStop(Color.Parse("#06FFFFFF"), 1));
        card.BorderBrush = borderGrad;

        var outerStack = new StackPanel();

        // ── Compact row ──────────────────────────────────────────────────
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));        // icon
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(14)));     // gap
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1.8, GridUnitType.Star)) { MinWidth = 120, MaxWidth = 220 }); // info
        row.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)) { MinWidth = 100 }); // sliders
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));        // chevron

        // Icon squircle (64×64)
        var iconBox = new Border
        {
            Width = 64, Height = 64,
            CornerRadius = new Avalonia.CornerRadius(14),
            VerticalAlignment = VerticalAlignment.Center
        };
        var iconGrad = new LinearGradientBrush { StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), EndPoint = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative) };
        iconGrad.GradientStops.Add(new GradientStop(Color.Parse("#1A2440"), 0));
        iconGrad.GradientStops.Add(new GradientStop(Color.Parse("#141820"), 1));
        iconBox.Background = iconGrad;
        iconBox.Child = new TextBlock
        {
            Text = "♪",
            FontSize = 26,
            Foreground = new SolidColorBrush(Color.Parse("#4F7CFF")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(iconBox, 0);

        // Info panel: name + connected + channel badge
        var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        var nameText = new TextBlock
        {
            FontSize = 16, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Colors.White),
            TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis,
            Text = dev.Name
        };
        dev.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(dev.Name)) nameText.Text = dev.Name; };

        var connDot = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 7, Height = 7,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 5, 0)
        };
        var connText = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };

        void UpdateConn()
        {
            bool ok = dev.IsConnected;
            connDot.Fill = new SolidColorBrush(Color.Parse(ok ? "#3DDC84" : "#4B4F6B"));
            connText.Text = ok ? "Connected" : "Disconnected";
            connText.Foreground = new SolidColorBrush(Color.Parse(ok ? "#8B8FA8" : "#FF453A"));
        }
        UpdateConn();
        dev.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(dev.IsConnected)) UpdateConn(); };

        var connRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Avalonia.Thickness(0, 4, 0, 0)
        };
        connRow.Children.Add(connDot);
        connRow.Children.Add(connText);

        // Channel badge
        var chanBadge = new Border
        {
            CornerRadius = new Avalonia.CornerRadius(10),
            Padding = new Avalonia.Thickness(8, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Avalonia.Thickness(0, 6, 0, 0)
        };
        var chanText = new TextBlock { FontSize = 11, FontWeight = FontWeight.Medium };

        void UpdateChan()
        {
            float pan = dev.Pan;
            if (pan <= -0.4f)
            {
                chanBadge.Background = new SolidColorBrush(Color.Parse("#284F7CFF"));
                chanText.Foreground  = new SolidColorBrush(Color.Parse("#4F7CFF"));
            }
            else if (pan >= 0.4f)
            {
                chanBadge.Background = new SolidColorBrush(Color.Parse("#289B7FFF"));
                chanText.Foreground  = new SolidColorBrush(Color.Parse("#9B7FFF"));
            }
            else
            {
                chanBadge.Background = new SolidColorBrush(Color.Parse("#288B8FA8"));
                chanText.Foreground  = new SolidColorBrush(Color.Parse("#8B8FA8"));
            }
            chanText.Text = dev.ChannelName;
        }
        UpdateChan();
        dev.PropertyChanged += (_, e) => { if (e.PropertyName is nameof(dev.Pan) or nameof(dev.ChannelName)) UpdateChan(); };
        chanBadge.Child = chanText;

        infoPanel.Children.Add(nameText);
        infoPanel.Children.Add(connRow);
        infoPanel.Children.Add(chanBadge);
        Grid.SetColumn(infoPanel, 2);

        // Volume + delay sliders
        var slidersPanel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(14, 0)
        };

        // Volume row
        var volRow = new Grid();
        volRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        volRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        volRow.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(40)));

        var volIcon = new TextBlock
        {
            Text = "♪", FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#4F7CFF")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        var volSlider = new Slider
        {
            Minimum = 0, Maximum = 100,
            Value = dev.VolumePercent,
            VerticalAlignment = VerticalAlignment.Center
        };
        volSlider.Classes.Add("SyncSlider");
        var volLabel = new TextBlock
        {
            FontSize = 11, FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Right,
            Text = $"{dev.VolumePercent:F0}%"
        };
        volSlider.ValueChanged += (_, e) =>
        {
            dev.VolumePercent = (float)e.NewValue;
            volLabel.Text = $"{e.NewValue:F0}%";
        };
        dev.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(dev.VolumePercent) && Math.Abs(volSlider.Value - dev.VolumePercent) > 0.5)
                volSlider.Value = dev.VolumePercent;
        };

        Grid.SetColumn(volIcon, 0); Grid.SetColumn(volSlider, 1); Grid.SetColumn(volLabel, 2);
        volRow.Children.Add(volIcon); volRow.Children.Add(volSlider); volRow.Children.Add(volLabel);

        // Delay row
        var delayRow = new Grid { Margin = new Avalonia.Thickness(0, 8, 0, 0) };
        delayRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        delayRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        delayRow.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(56)));

        var delayIcon = new TextBlock
        {
            Text = "⏱", FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#4F7CFF")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 8, 0)
        };
        var delaySlider = new Slider
        {
            Minimum = -600, Maximum = 600,
            Value = dev.DelayOffsetMs,
            VerticalAlignment = VerticalAlignment.Center
        };
        delaySlider.Classes.Add("SyncSlider");
        var delayLabel = new TextBlock
        {
            FontSize = 11, FontWeight = FontWeight.Medium,
            Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Right,
            Text = dev.DelayDisplayText
        };
        delaySlider.ValueChanged += (_, e) => dev.DelayOffsetMs = (float)e.NewValue;
        dev.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(dev.DelayOffsetMs))
            {
                if (Math.Abs(delaySlider.Value - dev.DelayOffsetMs) > 0.5)
                    delaySlider.Value = dev.DelayOffsetMs;
                delayLabel.Text = dev.DelayDisplayText;
            }
        };

        Grid.SetColumn(delayIcon, 0); Grid.SetColumn(delaySlider, 1); Grid.SetColumn(delayLabel, 2);
        delayRow.Children.Add(delayIcon); delayRow.Children.Add(delaySlider); delayRow.Children.Add(delayLabel);

        slidersPanel.Children.Add(volRow);
        slidersPanel.Children.Add(delayRow);
        Grid.SetColumn(slidersPanel, 3);

        // Chevron button
        var chevronText = new TextBlock
        {
            Text = expanded ? "▲" : "▼",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var chevronBtn = new Border
        {
            Width = 32, Height = 32,
            CornerRadius = new Avalonia.CornerRadius(8),
            Background = new SolidColorBrush(Color.Parse("#18FFFFFF")),
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = chevronText
        };
        Grid.SetColumn(chevronBtn, 4);

        row.Children.Add(iconBox);
        row.Children.Add(infoPanel);
        row.Children.Add(slidersPanel);
        row.Children.Add(chevronBtn);

        // ── Expanded section ──────────────────────────────────────────────
        var expandedSection = new StackPanel
        {
            IsVisible = expanded,
            Margin = new Avalonia.Thickness(0, 14, 0, 0)
        };

        expandedSection.Children.Add(new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.Parse("#1F2230")),
            Margin = new Avalonia.Thickness(0, 0, 0, 14)
        });

        // CHANNEL label + L/C/R segment
        expandedSection.Children.Add(new TextBlock
        {
            Text = "CHANNEL",
            FontSize = 10, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#4B4F6B")),
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        });

        var segRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Avalonia.Thickness(0, 0, 0, 16) };

        Button MakeSeg(string label, float panValue)
        {
            bool active = Math.Abs(dev.Pan - panValue) < 0.05f;
            var activeBg = panValue < -0.5f ? "#4F7CFF" : panValue > 0.5f ? "#9B7FFF" : "#4B4F6B";
            var btn = new Button
            {
                Content = label,
                Width = 44, Height = 32,
                FontSize = 13, FontWeight = FontWeight.Medium,
                Padding = new Avalonia.Thickness(0),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            btn.Classes.Add("Secondary");
            if (active)
                btn.Background = new SolidColorBrush(Color.Parse(activeBg));

            btn.Click += (_, _) =>
            {
                dev.Pan = panValue;
                foreach (var child in segRow.Children.OfType<Button>())
                {
                    float p = child.Content?.ToString() == "L" ? -1f : child.Content?.ToString() == "R" ? 1f : 0f;
                    bool isActive = Math.Abs(p - panValue) < 0.05f;
                    string bg = p < -0.5f ? "#4F7CFF" : p > 0.5f ? "#9B7FFF" : "#4B4F6B";
                    child.Background = isActive
                        ? new SolidColorBrush(Color.Parse(bg))
                        : new SolidColorBrush(Color.Parse("#252A35"));
                }
                DevicePersistence.Save(AppState.Instance.Devices);
            };
            return btn;
        }

        segRow.Children.Add(MakeSeg("L", -1f));
        segRow.Children.Add(MakeSeg("C", 0f));
        segRow.Children.Add(MakeSeg("R", 1f));
        expandedSection.Children.Add(segRow);

        // ADVANCED label
        expandedSection.Children.Add(new TextBlock
        {
            Text = "ADVANCED",
            FontSize = 10, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Color.Parse("#4B4F6B")),
            Margin = new Avalonia.Thickness(0, 0, 0, 8)
        });

        // Action buttons
        var actionsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };

        var testBtn = new Button { Padding = new Avalonia.Thickness(12, 0) };
        testBtn.Classes.Add("Ghost");
        testBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { new TextBlock { Text = "▶", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#4F7CFF")), VerticalAlignment = VerticalAlignment.Center },
                         new TextBlock { Text = "Test Sound", FontSize = 13, FontWeight = FontWeight.Medium, Foreground = new SolidColorBrush(Color.Parse("#4F7CFF")) } } };
        testBtn.Click += (_, _) =>
        {
            if (!AppState.Instance.EngineRunning) return;
        };

        var resetBtn = new Button { Padding = new Avalonia.Thickness(12, 0) };
        resetBtn.Classes.Add("Ghost");
        resetBtn.Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6,
            Children = { new TextBlock { Text = "↺", FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")), VerticalAlignment = VerticalAlignment.Center },
                         new TextBlock { Text = "Reset Sync", FontSize = 13, FontWeight = FontWeight.Medium, Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")) } } };
        resetBtn.Click += (_, _) =>
        {
            dev.DelayOffsetMs = 0f;
            DevicePersistence.Save(AppState.Instance.Devices);
        };

        var removeBtn = new Button { Padding = new Avalonia.Thickness(12, 0) };
        removeBtn.Classes.Add("Destructive");
        removeBtn.Content = new TextBlock { Text = "Remove", FontSize = 13, FontWeight = FontWeight.Medium };
        removeBtn.Click += (_, _) => AppState.Instance.Devices.Remove(dev);

        actionsRow.Children.Add(testBtn);
        actionsRow.Children.Add(resetBtn);
        actionsRow.Children.Add(removeBtn);
        expandedSection.Children.Add(actionsRow);

        // Chevron toggle
        chevronBtn.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            bool nowExpanded = !expandedSection.IsVisible;
            expandedSection.IsVisible = nowExpanded;
            chevronText.Text = nowExpanded ? "▲" : "▼";
            if (nowExpanded) _expandedCards.Add(dev.Id);
            else             _expandedCards.Remove(dev.Id);
        };

        outerStack.Children.Add(row);
        outerStack.Children.Add(expandedSection);
        card.Child = outerStack;

        // Hover
        card.PointerEntered += (_, _) =>
        {
            var hg = new LinearGradientBrush { StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative) };
            hg.GradientStops.Add(new GradientStop(Color.Parse("#222738"), 0));
            hg.GradientStops.Add(new GradientStop(Color.Parse("#161A24"), 1));
            card.Background = hg;
        };
        card.PointerExited += (_, _) =>
        {
            var g = new LinearGradientBrush { StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative), EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative) };
            g.GradientStops.Add(new GradientStop(Color.Parse("#1E2230"), 0));
            g.GradientStops.Add(new GradientStop(Color.Parse("#141820"), 1));
            card.Background = g;
        };

        // Click opens detail overlay
        card.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(card).Properties.IsRightButtonPressed) return;
            ShowDetail(dev);
        };

        return card;
    }

    // ── Sync card handlers ────────────────────────────────────────────────

    private void BtnBeat_Click(object? sender, RoutedEventArgs e) => BeatToggleRequested?.Invoke();
    private void BtnSyncNow_Click(object? sender, RoutedEventArgs e) => SyncNowRequested?.Invoke();
    private void BtnCalibrate_Click(object? sender, RoutedEventArgs e) => CalibrateRequested?.Invoke();

    private void DriftToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_driftLoading) return;
        AppState.Instance.DriftCorrection = DriftToggle.IsChecked == true;
    }

    // ── Add overlay ───────────────────────────────────────────────────────

    private void BtnAdd_Click(object? sender, RoutedEventArgs e)
    {
        TxtPickerName.Text = string.Empty;
        AddOverlay.IsVisible = true;
    }

    private void BtnAddClose_Click(object? sender, RoutedEventArgs e) => AddOverlay.IsVisible = false;

    private void AddBackdrop_Click(object? sender, PointerPressedEventArgs e) => AddOverlay.IsVisible = false;

    private void BtnAddConfirm_Click(object? sender, RoutedEventArgs e)
    {
        var name = TxtPickerName.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        AppState.Instance.Devices.Add(new SpeakerDevice
        {
            Name        = name,
            IsConnected = true
        });
        DevicePersistence.Save(AppState.Instance.Devices);
        AddOverlay.IsVisible = false;
        TxtPickerName.Text = string.Empty;
    }

    // ── Detail overlay ────────────────────────────────────────────────────

    private void ShowDetail(SpeakerDevice dev)
    {
        _detailDevice = dev;
        DetailName.Text = dev.Name;

        bool ok = dev.IsConnected;
        DetailStatusDot.Fill = new SolidColorBrush(Color.Parse(ok ? "#3DDC84" : "#FF453A"));
        DetailStatus.Text = ok ? "Connected" : "Disconnected";

        DetailTypeBadge.Background = new SolidColorBrush(Color.Parse("#252A35"));
        DetailType.Text = "Audio Output";
        DetailType.Foreground = new SolidColorBrush(Color.Parse("#8B8FA8"));

        DetailDelay.Text = dev.DelayDisplayText;
        DetailAngle.Text = $"{dev.AngleDegrees:F0}° — {AngleLabel(dev.AngleDegrees)}";

        DetailOverlay.IsVisible = true;
    }

    private static string AngleLabel(double a) => a switch
    {
        < 45 or >= 315 => "Front",
        < 135 => "Right",
        < 225 => "Back",
        _ => "Left"
    };

    private void BtnDetailClose_Click(object? sender, RoutedEventArgs e)
        => DetailOverlay.IsVisible = false;

    private void DetailBackdrop_Click(object? sender, PointerPressedEventArgs e)
        => DetailOverlay.IsVisible = false;

    private void BtnRemoveDevice_Click(object? sender, RoutedEventArgs e)
    {
        if (_detailDevice == null) return;
        AppState.Instance.Devices.Remove(_detailDevice);
        DevicePersistence.Save(AppState.Instance.Devices);
        _detailDevice = null;
        DetailOverlay.IsVisible = false;
    }
}
