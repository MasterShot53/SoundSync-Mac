using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using SoundSync.Mac.Audio;
using SoundSync.Models;

namespace SoundSync.Mac;

public partial class MainWindow : Window
{
    private readonly CoreAudioEngine _engine = new();
    private bool _engineRunning;

    public MainWindow()
    {
        InitializeComponent();
        ShowPage("Devices");
        _engine.StatusChanged += status => SidebarStatusText.Text = status;
    }

    // ── Navigation ───────────────────────────────────────────────────────────

    private void ShowPage(string name)
    {
        foreach (var btn in new[] { NavDevices, NavSync, NavSettings })
            btn.Classes.Remove("Active");

        switch (name)
        {
            case "Devices":  NavDevices.Classes.Add("Active");  break;
            case "Sync":     NavSync.Classes.Add("Active");     break;
            case "Settings": NavSettings.Classes.Add("Active"); break;
        }

        PageDevices.IsVisible  = name == "Devices";
        PageSync.IsVisible     = name == "Sync";
        PageSettings.IsVisible = name == "Settings";
    }

    private void NavDevices_Click(object? sender, RoutedEventArgs e)  => ShowPage("Devices");
    private void NavSync_Click(object? sender, RoutedEventArgs e)     => ShowPage("Sync");
    private void NavSettings_Click(object? sender, RoutedEventArgs e) => ShowPage("Settings");

    // ── Engine button ────────────────────────────────────────────────────────

    private void SidebarEngineBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_engineRunning)
        {
            _engine.Stop();
            BlackHoleManager.Release();
            _engineRunning = false;
            SidebarEngineBtnText.Text = "Start Engine";
            StatusDot.Fill = new SolidColorBrush(Color.Parse("#4B4F6B"));
        }
        else
        {
            if (!BlackHoleManager.IsInstalled()) { ShowPage("Sync"); return; }
            BlackHoleManager.Acquire();
            _engine.Start(AppState.Instance.Devices, autoCalibrate: false);
            _engineRunning = true;
            SidebarEngineBtnText.Text = "Stop Engine";
            StatusDot.Fill = new SolidColorBrush(Color.Parse("#3DDC84"));
        }
    }

    // ── Drag to move (macOS custom chrome) ───────────────────────────────────

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }
}
