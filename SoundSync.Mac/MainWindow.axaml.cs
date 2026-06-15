using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SoundSync.Audio;
using SoundSync.Mac.Audio;
using SoundSync.Models;

namespace SoundSync.Mac;

public partial class MainWindow : Window
{
    private readonly CoreAudioEngine _engine = new();

    public MainWindow()
    {
        InitializeComponent();

        // Wire DevicesView sync-card events
        PageDevices.CalibrateRequested += async () =>
        {
            AppState.Instance.IsCalibrating = true;
            PageDevices.BtnCalibrate.Content = "Calibrating…";
            await (_engine.CalibrateAsync(AppState.Instance.Devices));
            AppState.Instance.IsCalibrating = false;
            PageDevices.BtnCalibrate.Content = new Avalonia.Controls.TextBlock
            {
                Text = "Run Calibration",
                FontSize = 13,
                FontWeight = Avalonia.Media.FontWeight.SemiBold
            };
        };
        PageDevices.BeatToggleRequested += () =>
        {
            AppState.Instance.IsBeatPlaying = !AppState.Instance.IsBeatPlaying;
            if (AppState.Instance.IsBeatPlaying) _engine.StartBeat(); else _engine.StopBeat();
        };
        PageDevices.SyncNowRequested += () => _engine.SyncNow();

        // Load persisted state
        foreach (var d in DevicePersistence.Load())
            AppState.Instance.Devices.Add(d);
        AppSettingsPersistence.Load();

        // Restore volume slider
        SidebarVolumeSlider.Value = AppState.Instance.MasterVolume;
        SidebarVolLabel.Text = $"{(int)AppState.Instance.MasterVolume}%";

        // Engine status → sidebar
        _engine.StatusChanged += status =>
        {
            AppState.Instance.EngineStatus = status;
            SidebarStatusText.Text = status;
        };

        ShowPage("Devices");
        UpdateEngineCard();
    }

    protected override void OnClosed(EventArgs e)
    {
        DevicePersistence.Save(AppState.Instance.Devices);
        AppSettingsPersistence.Save();
        _engine.Dispose();
        base.OnClosed(e);
    }

    // ── Navigation ──────────────────────────────────────────────────────────

    private void ShowPage(string name)
    {
        foreach (var btn in new[] { NavDevices, NavSpatial, NavSettings })
            btn.Classes.Remove("Active");

        switch (name)
        {
            case "Devices":  NavDevices.Classes.Add("Active");  break;
            case "Spatial":  NavSpatial.Classes.Add("Active");  break;
            case "Settings": NavSettings.Classes.Add("Active"); break;
        }

        PageDevices.IsVisible  = name == "Devices";
        PageSpatial.IsVisible  = name == "Spatial";
        PageSettings.IsVisible = name == "Settings";
    }

    private void NavDevices_Click(object? sender, RoutedEventArgs e)  => ShowPage("Devices");
    private void NavSpatial_Click(object? sender, RoutedEventArgs e)  => ShowPage("Spatial");
    private void NavSettings_Click(object? sender, RoutedEventArgs e) => ShowPage("Settings");

    // ── Engine ──────────────────────────────────────────────────────────────

    private void SidebarEngineBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (AppState.Instance.EngineRunning)
        {
            _engine.Stop();
            BlackHoleManager.Release();
            AppState.Instance.EngineRunning = false;
            AppState.Instance.EngineStatus  = "Stopped";
        }
        else
        {
            if (!BlackHoleManager.IsInstalled()) { ShowPage("Settings"); return; }
            BlackHoleManager.Acquire();
            _engine.Start(AppState.Instance.Devices, autoCalibrate: AppState.Instance.AutoCalibrate);
            AppState.Instance.EngineRunning = true;
        }

        UpdateEngineCard();
        PageDevices.UpdateStatusCards();
    }

    private void UpdateEngineCard()
    {
        bool running = AppState.Instance.EngineRunning;

        SidebarEngineBtnText.Text = running ? "Stop Engine" : "Start Engine";
        SidebarStatusText.Text    = AppState.Instance.EngineStatus;
        StatusDot.Fill            = new SolidColorBrush(Color.Parse(running ? "#3DDC84" : "#4B4F6B"));

        SidebarEngineBtnText.Foreground = new SolidColorBrush(Color.Parse(running ? "#FFFFFF" : "#021A0A"));
        SidebarEngineBtn.Background     = new SolidColorBrush(Color.Parse(running ? "#FF453A" : "#00C853"));

        SidebarSourceName.IsVisible = running;
        SidebarSampleRate.IsVisible = running && AppState.Instance.AudioSourceRate > 0;
        SidebarSourceName.Text      = AppState.Instance.AudioSourceName;
        SidebarSampleRate.Text      = AppState.Instance.AudioRateText;

        BtnQaBeat.IsEnabled = running;
        BtnQaMute.IsEnabled = running;
        BtnQaTest.IsEnabled = running;
    }

    // ── Quick Actions ────────────────────────────────────────────────────────

    private void BtnQaBeat_Click(object? sender, RoutedEventArgs e)
    {
        AppState.Instance.IsBeatPlaying = !AppState.Instance.IsBeatPlaying;
        if (AppState.Instance.IsBeatPlaying) _engine.StartBeat(); else _engine.StopBeat();
        SidebarBeatBtnText.Text    = AppState.Instance.IsBeatPlaying ? "Stop Beat" : "Play Beat";
        PageDevices.BeatLabel.Text = SidebarBeatBtnText.Text;
    }

    private bool _muted;
    private void BtnQaMute_Click(object? sender, RoutedEventArgs e)
    {
        _muted = !_muted;
        SidebarMuteBtnText.Text = _muted ? "Unmute All" : "Mute All";
    }

    private void BtnQaTest_Click(object? sender, RoutedEventArgs e)
    {
        TestChannelStatus.Text = "";
        TestChannelOverlay.IsVisible = true;
    }

    // ── Test Channel modal ───────────────────────────────────────────────────

    private void TestChannelClose_Click(object? sender, RoutedEventArgs e)
        => TestChannelOverlay.IsVisible = false;

    private void TestChannelBackdrop_Click(object? sender, PointerPressedEventArgs e)
        => TestChannelOverlay.IsVisible = false;

    private void TestChannelCard_Click(object? sender, PointerPressedEventArgs e)
        => e.Handled = true;

    private void TestChannelL_Click(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        TestChannelStatus.Text = "Playing tone → LEFT channel";
    }

    private void TestChannelC_Click(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        TestChannelStatus.Text = "Playing tone → ALL channels";
    }

    private void TestChannelR_Click(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        TestChannelStatus.Text = "Playing tone → RIGHT channel";
    }

    // ── Toast ────────────────────────────────────────────────────────────────

    public void ShowToast(string message)
    {
        ToastMessage.Text = message;
        ToastCard.IsVisible = true;
    }

    private void ToastClose_Click(object? sender, RoutedEventArgs e)
        => ToastCard.IsVisible = false;

    // ── Volume ───────────────────────────────────────────────────────────────

    private void SidebarVolumeSlider_Changed(object? sender, RangeBaseValueChangedEventArgs e)
    {
        AppState.Instance.MasterVolume = (float)e.NewValue;
        SidebarVolLabel.Text = $"{(int)e.NewValue}%";
    }
}
