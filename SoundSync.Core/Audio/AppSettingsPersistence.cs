using System.IO;
using System.Text.Json;
using SoundSync.Models;

namespace SoundSync.Audio;

public static class AppSettingsPersistence
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SoundSync", "settings.json");

    private record Settings(bool AutoStart = false, bool AutoCalibrate = false, bool DriftCorrection = false, bool? FollowSystemTheme = null, int DriftCorrectionIntervalMs = 150_000);

    public static void Save()
    {
        try
        {
            var s = new Settings(
                AppState.Instance.AutoStart,
                AppState.Instance.AutoCalibrate,
                AppState.Instance.DriftCorrection,
                (bool?)AppState.Instance.FollowSystemTheme,
                AppState.Instance.DriftCorrectionIntervalMs);
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(s));
        }
        catch { }
    }

    public static void Load()
    {
        try
        {
            if (!File.Exists(_path)) return;
            var s = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path));
            if (s == null) return;
            AppState.Instance.AutoStart                  = s.AutoStart;
            AppState.Instance.AutoCalibrate              = s.AutoCalibrate;
            AppState.Instance.DriftCorrection            = s.DriftCorrection;
            AppState.Instance.FollowSystemTheme          = s.FollowSystemTheme ?? true;
            AppState.Instance.DriftCorrectionIntervalMs  = s.DriftCorrectionIntervalMs;
        }
        catch { }
    }
}
