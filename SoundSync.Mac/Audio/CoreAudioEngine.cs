using SoundSync.Audio;
using SoundSync.Models;

namespace SoundSync.Mac.Audio;

/// <summary>
/// Mac implementation of IAudioEngine using Core Audio / AVFoundation.
/// Mirrors the feature set of the Windows AudioEngine but uses macOS APIs.
/// BlackHole replaces VB-Audio Virtual Cable for virtual audio routing.
/// TODO: Implement using AudioToolbox / AVAudioEngine P/Invoke or a Mac audio library.
/// </summary>
public class CoreAudioEngine : IAudioEngine
{
    public bool IsRunning { get; private set; }

    public event Action<string>? StatusChanged;
#pragma warning disable CS0067
    public event Action<string>? Error;
    public event Action<Dictionary<string, float>>? CalibrationApplied;
#pragma warning restore CS0067

    public void Start(IEnumerable<SpeakerDevice> devices, bool autoCalibrate)
    {
        // TODO: init AVAudioEngine, enumerate Core Audio render endpoints,
        // set up tap on BlackHole output, route to physical speakers with delay buffers.
        try { AppState.Instance.AvailableOutputs = BlackHoleManager.GetOutputDeviceNames(); } catch { }
        IsRunning = true;
        StatusChanged?.Invoke($"Running — {devices.Count()} speaker(s)");
    }

    public void Stop()
    {
        IsRunning = false;
        StatusChanged?.Invoke("Stopped");
    }

    public void AddDevice(SpeakerDevice device)    { /* TODO */ }
    public void RemoveDevice(string deviceId)       { /* TODO */ }

    public void SyncNow()                           { /* TODO: re-prime delay buffers */ }
    public void StartDriftCorrection()              { /* TODO: periodic SyncNow timer */ }
    public void StopDriftCorrection()               { /* TODO */ }

    public void StartBeat(int bpm = 80)             { /* TODO: generate click via AVAudioPlayerNode */ }
    public void StopBeat()                          { /* TODO */ }

    public void StartAutoCalibrate()                { /* TODO */ }
    public void StopAutoCalibrate()                 { /* TODO */ }

    public Task<Dictionary<string, float>> CalibrateAsync(
        IEnumerable<SpeakerDevice> devices,
        IProgress<string>? progress = null,
        CancellationToken ct = default,
        bool quietMode = false)
    {
        // TODO: record mic via AVAudioEngine, play click through each device,
        // pass samples to CalibrationMath.FindArrivalTime, return offsets.
        progress?.Report("Calibration not yet implemented on Mac.");
        return Task.FromResult(new Dictionary<string, float>());
    }

    public void PlayTestTone(char channel)          { /* TODO: AVAudioPlayerNode sine wave */ }
    public void CancelTestTone()                    { /* TODO */ }
    public void PlayTestToneToDevice(string id)     { /* TODO */ }

    public float GetDeviceLevel(string deviceId) => 0f;

    public void Dispose()
    {
        Stop();
    }
}
