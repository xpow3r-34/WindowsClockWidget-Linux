using System.Diagnostics;
using System.IO;

namespace WindowsClockWidget;

public sealed class SoundPlayer : IDisposable
{
    private readonly string _path;
    private Process? _process;

    public SoundPlayer(string path)
    {
        _path = path;
    }

    public static string DefaultChimePath => EnsureDefaultChime();

    public void Load()
    {
        if (!File.Exists(_path))
        {
            throw new FileNotFoundException("Ses dosyası bulunamadı.", _path);
        }
    }

    public void Play()
    {
        Start(false);
    }

    public void PlaySync()
    {
        Start(true);
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill();
            }
            _process?.Dispose();
        }
        catch
        {
        }
        _process = null;
    }

    private void Start(bool wait)
    {
        if (!File.Exists(_path))
        {
            throw new FileNotFoundException("Ses dosyası bulunamadı.", _path);
        }
        var player = PreferredPlayer();
        if (string.IsNullOrEmpty(player))
        {
            throw new FileNotFoundException("Ses çalıcı bulunamadı.", "aplay");
        }
        var startInfo = new ProcessStartInfo(player)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_path);
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Ses çalıcı başlatılamadı.");
        _process = process;
        if (wait)
        {
            process.WaitForExit();
        }
    }

    private static string PreferredPlayer()
    {
        var candidates = new[] { "paplay", "aplay" };
        foreach (var candidate in candidates)
        {
            var paths = new[] { $"/usr/bin/{candidate}", $"/bin/{candidate}" };
            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }
        }
        return "";
    }

    public static string EnsureDefaultChime()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowsClockWidget");
        var path = Path.Combine(dir, "chimes.wav");
        if (File.Exists(path))
        {
            return path;
        }
        try
        {
            Directory.CreateDirectory(dir);
            const int sampleRate = 22050;
            const double duration = 0.9;
            using var stream = File.Create(path);
            using var writer = new BinaryWriter(stream);
            var samples = (int)(sampleRate * duration);
            writer.Write(new byte[] { 0x52, 0x49, 0x46, 0x46 });
            writer.Write(36 + samples * 2);
            writer.Write(new byte[] { 0x57, 0x41, 0x56, 0x45 });
            writer.Write(new byte[] { 0x66, 0x6D, 0x74, 0x20 });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new byte[] { 0x64, 0x61, 0x74, 0x61 });
            writer.Write(samples * 2);
            for (var i = 0; i < samples; i++)
            {
                var second = i / (double)sampleRate;
                var envelope = Math.Exp(-3.5 * second);
                var frequency = second < 0.35 ? 880 : 1174;
                var value = (short)(12000 * envelope * Math.Sin(2 * Math.PI * frequency * second));
                writer.Write(value);
            }
            return path;
        }
        catch
        {
            return "";
        }
    }

    public static string[] SoundFiles()
    {
        var result = new List<string> { DefaultChimePath };
        var roots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share", "sounds"),
            "/usr/share/sounds"
        };
        foreach (var root in roots)
        {
            try
            {
                if (Directory.Exists(root))
                {
                    result.AddRange(Directory.GetFiles(root, "*.wav", SearchOption.AllDirectories));
                }
            }
            catch
            {
            }
        }
        return result.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
