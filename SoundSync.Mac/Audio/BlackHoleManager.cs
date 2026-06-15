using System.Runtime.InteropServices;
using System.Text;

namespace SoundSync.Mac.Audio;

/// <summary>
/// Manages the BlackHole virtual audio driver on macOS.
/// Mirrors VirtualCableManager on Windows: acquires BlackHole as the system
/// default output on Start, restores the original device on Stop or crash.
///
/// BlackHole is a free, open-source virtual audio loopback driver:
/// https://github.com/ExistentialAudio/BlackHole
/// </summary>
public static class BlackHoleManager
{
    private const string BlackHoleDriverPath  = "/Library/Audio/Plug-Ins/HAL/BlackHole2ch.driver";
    private const string BlackHoleDeviceName  = "BlackHole 2ch";
    private const string BlackHolePkgUrl      = "https://github.com/ExistentialAudio/BlackHole/releases/download/v0.6.0/BlackHole2ch.pkg";
    private const string BlackHolePkgTmp      = "/tmp/SoundSync_BlackHole2ch.pkg";

    private static readonly string RestoreFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SoundSync", "restore_device.txt");

    private static uint _savedDeviceId;

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the BlackHole HAL driver is installed.
    /// Uses a filesystem check — faster than enumerating Core Audio devices.
    /// </summary>
    public static bool IsInstalled() => Directory.Exists(BlackHoleDriverPath);

    /// <summary>
    /// Downloads BlackHole 2ch and installs it via a macOS admin-auth prompt.
    /// The user will see one native password dialog — no other prompts.
    /// </summary>
    public static async Task InstallAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Downloading BlackHole audio driver…");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "SoundSync");
        var bytes = await http.GetByteArrayAsync(BlackHolePkgUrl);
        await File.WriteAllBytesAsync(BlackHolePkgTmp, bytes);

        progress?.Report("Installing BlackHole (admin permission required)…");

        // osascript elevates via macOS native auth dialog — user sees one password prompt
        var result = await RunAsync("osascript",
            $"-e 'do shell script \"installer -pkg {BlackHolePkgTmp} -target /\" " +
            $"with administrator privileges'");

        File.Delete(BlackHolePkgTmp);

        if (result.ExitCode != 0)
            throw new Exception($"BlackHole install failed: {result.StdErr}");

        progress?.Report("BlackHole installed successfully.");
    }

    /// <summary>
    /// Switches the macOS system default output to BlackHole and saves
    /// the current device ID to a restore file for crash recovery.
    /// </summary>
    public static void Acquire()
    {
        if (!IsInstalled())
            throw new InvalidOperationException(
                "BlackHole is not installed. Call InstallAsync() first.");

        uint blackHoleId = GetDeviceIdByName(BlackHoleDeviceName);
        if (blackHoleId == 0)
            throw new InvalidOperationException(
                "BlackHole driver is installed but not visible to Core Audio yet. " +
                "A logout/login may be required.");

        _savedDeviceId = GetDefaultOutputDevice();

        Directory.CreateDirectory(Path.GetDirectoryName(RestoreFile)!);
        File.WriteAllText(RestoreFile, _savedDeviceId.ToString());

        SetDefaultOutputDevice(blackHoleId);
    }

    /// <summary>
    /// Restores the saved system default output and deletes the restore file.
    /// </summary>
    public static void Release()
    {
        if (_savedDeviceId == 0) return;
        try
        {
            SetDefaultOutputDevice(_savedDeviceId);
            DeleteRestoreFile();
        }
        catch { }
        _savedDeviceId = 0;
    }

    /// <summary>
    /// Called at startup. If the app crashed while BlackHole was active,
    /// restores the original device.
    /// </summary>
    public static void RecoverFromCrash()
    {
        if (!File.Exists(RestoreFile)) return;
        try
        {
            string text = File.ReadAllText(RestoreFile).Trim();
            if (!uint.TryParse(text, out uint savedId) || savedId == 0) return;

            uint blackHoleId = GetDeviceIdByName(BlackHoleDeviceName);
            uint current = GetDefaultOutputDevice();
            if (current == blackHoleId)
                SetDefaultOutputDevice(savedId);

            DeleteRestoreFile();
        }
        catch { }
    }

    /// <summary>
    /// Uninstalls the BlackHole driver. Requires admin auth.
    /// Called from SettingsView when the user chooses to uninstall SoundSync.
    /// </summary>
    public static async Task UninstallBlackHoleAsync()
    {
        // Forget the package receipt and remove the HAL driver
        string script =
            "do shell script \"" +
            "pkgutil --forget com.existentialAudio.BlackHole.BlackHole2ch 2>/dev/null; " +
            "rm -rf /Library/Audio/Plug-Ins/HAL/BlackHole2ch.driver" +
            "\" with administrator privileges";

        var result = await RunAsync("osascript", $"-e '{script}'");
        if (result.ExitCode != 0)
            throw new Exception($"BlackHole uninstall failed: {result.StdErr}");
    }

    // ── Core Audio P/Invoke ──────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct AudioObjectPropertyAddress
    {
        public uint mSelector;
        public uint mScope;
        public uint mElement;
    }

    private const uint kAudioObjectSystemObject              = 1;
    private const uint kAudioHardwarePropertyDevices         = 0x64657623; // 'dev#'
    private const uint kAudioHardwarePropertyDefaultOutput   = 0x644F7574; // 'dOut'
    private const uint kAudioObjectPropertyScopeGlobal       = 0x676C6F62; // 'glob'
    private const uint kAudioObjectPropertyScopeOutput       = 0x6F757470; // 'outp'
    private const uint kAudioObjectPropertyElementMain       = 0;
    private const uint kAudioObjectPropertyName              = 0x6C6E616D; // 'lnam'
    private const uint kAudioDevicePropertyStreams           = 0x73746D23; // 'stm#'
    private const uint kCFStringEncodingUTF8                 = 0x08000100;

    private const string CoreAudio        = "/System/Library/Frameworks/CoreAudio.framework/CoreAudio";
    private const string CoreFoundation   = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyDataSize(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        uint qualifierSize,
        IntPtr qualifier,
        out uint dataSize);

    [DllImport(CoreAudio)]
    private static extern int AudioObjectGetPropertyData(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        uint qualifierSize,
        IntPtr qualifier,
        ref uint dataSize,
        IntPtr data);

    [DllImport(CoreAudio)]
    private static extern int AudioObjectSetPropertyData(
        uint objectId,
        ref AudioObjectPropertyAddress address,
        uint qualifierSize,
        IntPtr qualifier,
        uint dataSize,
        ref uint data);

    [DllImport(CoreFoundation)]
    private static extern bool CFStringGetCString(
        IntPtr str, byte[] buffer, long bufferSize, uint encoding);

    [DllImport(CoreFoundation)]
    private static extern void CFRelease(IntPtr cf);

    private static uint GetDefaultOutputDevice()
    {
        var addr = DefaultOutputAddress();
        uint dataSize = sizeof(uint);
        IntPtr buf = Marshal.AllocHGlobal(sizeof(uint));
        try
        {
            AudioObjectGetPropertyData(kAudioObjectSystemObject, ref addr, 0, IntPtr.Zero, ref dataSize, buf);
            return (uint)Marshal.ReadInt32(buf);
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    private static void SetDefaultOutputDevice(uint deviceId)
    {
        var addr = DefaultOutputAddress();
        AudioObjectSetPropertyData(kAudioObjectSystemObject, ref addr, 0, IntPtr.Zero, sizeof(uint), ref deviceId);
    }

    private static AudioObjectPropertyAddress DefaultOutputAddress() => new()
    {
        mSelector = kAudioHardwarePropertyDefaultOutput,
        mScope    = kAudioObjectPropertyScopeGlobal,
        mElement  = kAudioObjectPropertyElementMain
    };

    private static uint[] GetAllDeviceIds()
    {
        var addr = new AudioObjectPropertyAddress
        {
            mSelector = kAudioHardwarePropertyDevices,
            mScope    = kAudioObjectPropertyScopeGlobal,
            mElement  = kAudioObjectPropertyElementMain
        };

        AudioObjectGetPropertyDataSize(kAudioObjectSystemObject, ref addr, 0, IntPtr.Zero, out uint dataSize);
        int count = (int)(dataSize / sizeof(uint));
        if (count == 0) return Array.Empty<uint>();

        IntPtr buf = Marshal.AllocHGlobal((int)dataSize);
        try
        {
            AudioObjectGetPropertyData(kAudioObjectSystemObject, ref addr, 0, IntPtr.Zero, ref dataSize, buf);
            var ids = new uint[count];
            for (int i = 0; i < count; i++)
                ids[i] = (uint)Marshal.ReadInt32(buf, i * sizeof(uint));
            return ids;
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    private static string GetDeviceName(uint deviceId)
    {
        var addr = new AudioObjectPropertyAddress
        {
            mSelector = kAudioObjectPropertyName,
            mScope    = kAudioObjectPropertyScopeGlobal,
            mElement  = kAudioObjectPropertyElementMain
        };

        uint dataSize = (uint)IntPtr.Size;
        IntPtr buf = Marshal.AllocHGlobal(IntPtr.Size);
        try
        {
            int err = AudioObjectGetPropertyData(deviceId, ref addr, 0, IntPtr.Zero, ref dataSize, buf);
            if (err != 0) return string.Empty;

            IntPtr cfStr = Marshal.ReadIntPtr(buf);
            if (cfStr == IntPtr.Zero) return string.Empty;

            try
            {
                var bytes = new byte[256];
                CFStringGetCString(cfStr, bytes, bytes.Length, kCFStringEncodingUTF8);
                return Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            }
            finally { CFRelease(cfStr); }
        }
        finally { Marshal.FreeHGlobal(buf); }
    }

    private static uint GetDeviceIdByName(string name)
    {
        foreach (uint id in GetAllDeviceIds())
            if (GetDeviceName(id).Equals(name, StringComparison.OrdinalIgnoreCase))
                return id;
        return 0;
    }

    private static bool HasOutputStreams(uint deviceId)
    {
        var addr = new AudioObjectPropertyAddress
        {
            mSelector = kAudioDevicePropertyStreams,
            mScope    = kAudioObjectPropertyScopeOutput,
            mElement  = kAudioObjectPropertyElementMain
        };
        int err = AudioObjectGetPropertyDataSize(deviceId, ref addr, 0, IntPtr.Zero, out uint dataSize);
        return err == 0 && dataSize > 0;
    }

    /// <summary>
    /// Returns the names of all macOS audio output devices, excluding BlackHole.
    /// Used to populate the Add Speaker picker in DevicesView.
    /// </summary>
    public static string[] GetOutputDeviceNames()
    {
        var names = new List<string>();
        foreach (uint id in GetAllDeviceIds())
        {
            if (!HasOutputStreams(id)) continue;
            string name = GetDeviceName(id);
            if (string.IsNullOrEmpty(name)) continue;
            if (name.Equals(BlackHoleDeviceName, StringComparison.OrdinalIgnoreCase)) continue;
            names.Add(name);
        }
        return names.ToArray();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void DeleteRestoreFile()
    {
        try { if (File.Exists(RestoreFile)) File.Delete(RestoreFile); } catch { }
    }

    private static async Task<(int ExitCode, string StdErr)> RunAsync(string exe, string args)
    {
        using var proc = new System.Diagnostics.Process();
        proc.StartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = exe,
            Arguments              = args,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };
        proc.Start();
        string err = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        return (proc.ExitCode, err);
    }
}
