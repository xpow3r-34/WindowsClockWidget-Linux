using Avalonia.Media;

namespace WindowsClockWidget;

internal static class UiTheme
{
    public static SolidColorBrush Brush(string hex, double opacity = 1)
    {
        try
        {
            var color = Color.Parse(hex.StartsWith('#') ? hex : "#" + hex);
            return new SolidColorBrush(color, opacity);
        }
        catch
        {
            return new SolidColorBrush(Color.FromRgb(0x1F, 0x1E, 0x1D), opacity);
        }
    }

    public static FontWeight Weight(string weight) => weight switch
    {
        "Thin" => FontWeight.Thin,
        "ExtraLight" => FontWeight.ExtraLight,
        "Light" => FontWeight.Light,
        "Normal" => FontWeight.Normal,
        "Medium" => FontWeight.Medium,
        "SemiBold" => FontWeight.SemiBold,
        "Bold" => FontWeight.Bold,
        _ => FontWeight.Normal
    };

    public static string Hex(SolidColorBrush brush)
        => $"#{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}";
}
