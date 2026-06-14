using Avalonia.Controls;
using Avalonia.Interactivity;
using SoundSync.Mac.Audio;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class SyncView : UserControl
{
    private readonly CoreAudioEngine _engine = new();
    private bool _beatRunning;
    private bool _driftRunning;

    public SyncView()
    {
        InitializeComponent();
        _engine.StatusChanged += s => EngineStatusText.Text = s;
        RefreshBlackHoleBanner();
    }

    private void RefreshBlackHoleBanner()
    {
        BlackHoleBanner.IsVisible = !BlackHoleManager.IsInstalled();
    }

    private async void BtnGetBlackHole_Click(object? sender, RoutedEventArgs e)
    {
        BtnGetBlackHole.IsEnabled = false;
        BtnGetBlackHole.Content = "Installing…";
        EngineStatusText.Text = "Downloading BlackHole audio driver…";

        try
        {
            var progress = new Progress<string>(msg => EngineStatusText.Text = msg);
            await BlackHoleManager.InstallAsync(progress);
            RefreshBlackHoleBanner();
            EngineStatusText.Text = "BlackHole installed. You can now start the engine.";
        }
        catch (Exception ex)
        {
            EngineStatusText.Text = $"Install failed: {ex.Message}";
            BtnGetBlackHole.IsEnabled = true;
            BtnGetBlackHole.Content = "Retry Install";
        }
    }

    private void BtnBeat_Click(object? sender, RoutedEventArgs e)
    {
        _beatRunning = !_beatRunning;
        BtnBeat.Content = _beatRunning ? "Stop Beat" : "Play Beat";
        if (_beatRunning) _engine.StartBeat(); else _engine.StopBeat();
    }

    private void BtnAutoCalibrate_Click(object? sender, RoutedEventArgs e)
    {
        var on = ((ToggleButton)sender!).IsChecked == true;
        BtnAutoCalibrate.Content = on ? "Auto: On" : "Auto: Off";
        if (on) _engine.StartAutoCalibrate(); else _engine.StopAutoCalibrate();
    }

    private async void BtnCalibrate_Click(object? sender, RoutedEventArgs e)
    {
        BtnCalibrate.IsEnabled = false;
        BtnCalibrate.Content = "Calibrating…";
        await _engine.CalibrateAsync(AppState.Instance.Devices);
        BtnCalibrate.Content = "Calibrate Now";
        BtnCalibrate.IsEnabled = true;
    }

    private void BtnSyncNow_Click(object? sender, RoutedEventArgs e)       => _engine.SyncNow();

    private void BtnDriftCorrection_Click(object? sender, RoutedEventArgs e)
    {
        _driftRunning = !_driftRunning;
        BtnDriftCorrection.Content = _driftRunning ? "On" : "Off";
        if (_driftRunning) _engine.StartDriftCorrection(); else _engine.StopDriftCorrection();
    }
}
