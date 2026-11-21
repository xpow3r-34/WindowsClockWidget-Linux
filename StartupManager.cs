using System.IO;
using System.Text;

namespace WindowsClockWidget;

internal static class StartupManager
{
    private static string DesktopFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".config", "autostart", "windowsclockwidget.desktop");

    public static void Set(bool enabled)
    {
        try
        {
            var path = DesktopFile;
            if (!enabled)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var exe = Environment.ProcessPath ?? "";
            if (string.IsNullOrWhiteSpace(exe)) return;
            var content = new StringBuilder()
                .AppendLine("[Desktop Entry]")
                .AppendLine("Type=Application")
                .AppendLine("Name=WindowsClockWidget")
                .AppendLine("Comment=Transparent desktop clock widget")
                .AppendLine($"Exec={exe}")
                .AppendLine("Terminal=false")
                .AppendLine("X-GNOME-Autostart-enabled=true")
                .ToString();
            File.WriteAllText(path, content, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }
}
