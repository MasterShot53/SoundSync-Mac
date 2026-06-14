using System.IO;
using System.Text.Json;
using SoundSync.Models;

namespace SoundSync.Audio;

public static class DevicePersistence
{
    private static readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SoundSync", "devices.json");

    private record DeviceRecord(
        string Id, string Name, string? WindowsDeviceId,
        float DelayOffsetMs, float VolumePercent, double AngleDegrees, float Pan = 0f);

    public static void Save(IEnumerable<SpeakerDevice> devices)
    {
        try
        {
            var records = devices.Select(d => new DeviceRecord(
                d.Id, d.Name, d.WindowsDeviceId,
                d.DelayOffsetMs, d.VolumePercent, d.AngleDegrees, d.Pan));
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(records,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    public static List<SpeakerDevice> Load()
    {
        try
        {
            if (!File.Exists(_path)) return new();
            var records = JsonSerializer.Deserialize<List<DeviceRecord>>(File.ReadAllText(_path));
            if (records == null) return new();
            return records.Select(r => new SpeakerDevice
            {
                Id              = r.Id,
                Name            = r.Name,
                WindowsDeviceId = r.WindowsDeviceId,
                IsConnected     = true,
                DelayOffsetMs   = r.DelayOffsetMs,
                VolumePercent   = r.VolumePercent,
                AngleDegrees    = r.AngleDegrees,
                Pan             = r.Pan
            }).ToList();
        }
        catch { return new(); }
    }
}
