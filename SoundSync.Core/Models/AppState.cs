using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundSync.Models;

public class AppState : INotifyPropertyChanged
{
    public static AppState Instance { get; } = new();

    private AppState()
    {
        Devices.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ConnectedCount));
    }

    public ObservableCollection<SpeakerDevice> Devices { get; } = new();

    private bool   _engineRunning;
    private bool   _spatialEnabled;
    private string _engineStatus   = "Stopped";
    private bool   _isCalibrating;
    private bool   _isBeatPlaying;
    private bool   _autoCalibrate  = false;
    private bool   _autoStart      = false;
    private bool   _driftCorrection = false;
    private int    _driftCorrectionIntervalMs = 150_000;
    private bool   _followSystemTheme = true;
    private bool   _virtualCableReady;
    private float  _masterVolume   = 100f;
    private string _audioSourceName = "No Source";
    private int    _audioSourceRate  = 0;
    private float  _syncDiffMs      = 0f;

    public bool EngineRunning
    {
        get => _engineRunning;
        set { _engineRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(EngineButtonText)); }
    }

    public bool SpatialEnabled
    {
        get => _spatialEnabled;
        set { _spatialEnabled = value; OnPropertyChanged(); }
    }

    public string EngineStatus
    {
        get => _engineStatus;
        set { _engineStatus = value; OnPropertyChanged(); }
    }

    public bool IsCalibrating
    {
        get => _isCalibrating;
        set { _isCalibrating = value; OnPropertyChanged(); }
    }

    public bool IsBeatPlaying
    {
        get => _isBeatPlaying;
        set { _isBeatPlaying = value; OnPropertyChanged(); }
    }

    public bool AutoCalibrate
    {
        get => _autoCalibrate;
        set { _autoCalibrate = value; OnPropertyChanged(); }
    }

    public bool AutoStart
    {
        get => _autoStart;
        set { _autoStart = value; OnPropertyChanged(); }
    }

    public bool DriftCorrection
    {
        get => _driftCorrection;
        set { _driftCorrection = value; OnPropertyChanged(); }
    }

    public int DriftCorrectionIntervalMs
    {
        get => _driftCorrectionIntervalMs;
        set { _driftCorrectionIntervalMs = value; OnPropertyChanged(); }
    }

    public bool FollowSystemTheme
    {
        get => _followSystemTheme;
        set { _followSystemTheme = value; OnPropertyChanged(); }
    }

    public bool VirtualCableReady
    {
        get => _virtualCableReady;
        set { _virtualCableReady = value; OnPropertyChanged(); }
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set { _masterVolume = Math.Clamp(value, 0f, 100f); OnPropertyChanged(); }
    }

    public string AudioSourceName
    {
        get => _audioSourceName;
        set { _audioSourceName = value; OnPropertyChanged(); OnPropertyChanged(nameof(AudioRateText)); }
    }

    public int AudioSourceRate
    {
        get => _audioSourceRate;
        set { _audioSourceRate = value; OnPropertyChanged(); OnPropertyChanged(nameof(AudioRateText)); }
    }

    public float SyncDiffMs
    {
        get => _syncDiffMs;
        set { _syncDiffMs = value; OnPropertyChanged(); }
    }

    private DateTime? _lastCalibrationTime;
    public DateTime? LastCalibrationTime
    {
        get => _lastCalibrationTime;
        set { _lastCalibrationTime = value; OnPropertyChanged(); }
    }

    public string? DefaultWindowsDeviceId { get; set; }

    private string[] _availableOutputs = Array.Empty<string>();
    public string[] AvailableOutputs
    {
        get => _availableOutputs;
        set { _availableOutputs = value; OnPropertyChanged(); }
    }

    // Computed properties
    public string EngineButtonText => _engineRunning ? "Stop Engine" : "Start Engine";

    public string AudioRateText =>
        _audioSourceRate > 0 ? $"{_audioSourceRate / 1000.0:F1} kHz" : string.Empty;

    public int ConnectedCount => Devices.Count;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
