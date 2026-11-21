using Avalonia;

namespace WindowsClockWidget;

internal static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        _mutex = new Mutex(true, @"Local\WindowsClockWidget_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
