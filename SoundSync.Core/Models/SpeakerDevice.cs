using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundSync.Models;

public class SpeakerDevice : INotifyPropertyChanged
{
    private string _name = "Unknown Speaker";
    private bool   _isConnected;
    private float  _delayOffsetMs;
    private float  _volumePercent = 100f;
    private float  _pan           = 0f;
    private double _angleDegrees  = 270;
    private bool   _isActive;
    private string _statusDetail  = "";
    private bool   _isPassthrough;

    public string  Id              { get; set; } = Guid.NewGuid().ToString();
    public string? WindowsDeviceId { get; set; }
    public string? MacAddress      { get; set; }
    public string? Codec           { get; set; }
    public int     Index           { get; set; }

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusDot));
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(ActiveDot)); }
    }

    public string ActiveDot => _isActive ? "#3DDC84" : "#FF453A";

    public bool IsPassthrough
    {
        get => _isPassthrough;
        set { _isPassthrough = value; OnPropertyChanged(); }
    }

    public string StatusDetail
    {
        get => _statusDetail;
        set { _statusDetail = value; OnPropertyChanged(); }
    }

    public float VolumePercent
    {
        get => _volumePercent;
        set
        {
            _volumePercent = Math.Clamp(value, 0f, 100f);
            OnPropertyChanged();
            OnPropertyChanged(nameof(VolumeDisplayText));
        }
    }

    // -1 = left only, 0 = full stereo, +1 = right only
    public float Pan
    {
        get => _pan;
        set
        {
            _pan = Math.Clamp(value, -1f, 1f);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ChannelName));
        }
    }

    public float DelayOffsetMs
    {
        get => _delayOffsetMs;
        set
        {
            _delayOffsetMs = Math.Clamp(value, -600f, 600f);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DelayDisplayText));
        }
    }

    // 0° = front, 90° = right, 180° = back, 270° = left
    public double AngleDegrees
    {
        get => _angleDegrees;
        set
        {
            _angleDegrees = ((value % 360) + 360) % 360;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AngleDisplayText));
            OnPropertyChanged(nameof(LeftGain));
            OnPropertyChanged(nameof(RightGain));
        }
    }

    // Equal-power panning: 270° = pure left, 90° = pure right
    public float LeftGain
    {
        get
        {
            double rad = (_angleDegrees - 270.0) * Math.PI / 180.0;
            return (float)Math.Sqrt(Math.Max(0, Math.Cos(rad)));
        }
    }

    public float RightGain
    {
        get
        {
            double rad = (_angleDegrees - 90.0) * Math.PI / 180.0;
            return (float)Math.Sqrt(Math.Max(0, Math.Cos(rad)));
        }
    }

    /// <summary>Returns "Left Channel", "Center Channel", or "Right Channel" based on Pan value.</summary>
    public string ChannelName => _pan <= -0.4f ? "Left Channel"
                               : _pan >= 0.4f  ? "Right Channel"
                               :                 "Center Channel";

    public string VolumeDisplayText => $"{VolumePercent:F0}%";
    public string StatusText        => IsConnected ? "Connected" : "Disconnected";
    public string StatusDot         => IsConnected ? "#3DDC84" : "#FF453A";
    public string DelayDisplayText  => DelayOffsetMs == 0 ? "0 ms"
        : DelayOffsetMs > 0 ? $"+{DelayOffsetMs:F0} ms"
        : $"{DelayOffsetMs:F0} ms";
    public string AngleDisplayText  => $"{AngleDegrees:F0}°";
    public string ShortLabel        => Name.Length > 0 ? Name[..Math.Min(2, Name.Length)].ToUpper() : "??";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
