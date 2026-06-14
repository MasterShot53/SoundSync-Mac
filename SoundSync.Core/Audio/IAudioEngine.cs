using SoundSync.Models;

namespace SoundSync.Audio;

public interface IAudioEngine : IDisposable
{
    bool IsRunning { get; }

    event Action<string>? StatusChanged;
    event Action<string>? Error;
    event Action<Dictionary<string, float>>? CalibrationApplied;

    void Start(IEnumerable<SpeakerDevice> devices, bool autoCalibrate);
    void Stop();

    void AddDevice(SpeakerDevice device);
    void RemoveDevice(string deviceId);

    void SyncNow();
    void StartDriftCorrection();
    void StopDriftCorrection();

    void StartBeat(int bpm = 80);
    void StopBeat();

    void StartAutoCalibrate();
    void StopAutoCalibrate();

    Task<Dictionary<string, float>> CalibrateAsync(
        IEnumerable<SpeakerDevice> devices,
        IProgress<string>? progress = null,
        CancellationToken ct = default,
        bool quietMode = false);

    void PlayTestTone(char channel);
    void CancelTestTone();
    void PlayTestToneToDevice(string deviceId);

    float GetDeviceLevel(string deviceId);
}
