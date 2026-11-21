using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;

namespace WindowsClockWidget;

public class SettingsWindow : Window
{
    public AppSettings Result { get; private set; }
    public bool Accepted { get; private set; }
    public Action<AppSettings>? PreviewRequested { get; set; }
    public Action<AppSettings>? ApplyRequested { get; set; }

    private readonly AppSettings _original;
    private readonly Dictionary<string, string> _credentialDrafts = new(StringComparer.OrdinalIgnoreCase);
    private string _activeCredentialSource = "OpenMeteo";
    private bool _controlsReady;

    private readonly ComboBox _hourFont, _minuteFont, _dateFont;
    private readonly ComboBox _hourWeight, _minuteWeight, _dateWeight;
    private readonly ComboBox _sourceBox, _refreshBox, _credentialBoxHost, _cityBox, _positionBox, _datePositionBox;
    private readonly ComboBox _soundBox;
    private readonly Slider _hourSize, _minuteSize, _weatherSize, _dateSize;
    private readonly Slider _weatherOffsetX, _weatherOffsetY, _dateOffsetX, _dateOffsetY;
    private readonly TextBlock _hourSizeLabel, _minuteSizeLabel, _weatherSizeLabel, _dateSizeLabel;
    private readonly TextBlock _weatherOffsetXLabel, _weatherOffsetYLabel, _dateOffsetXLabel, _dateOffsetYLabel;
    private readonly TextBlock _credentialLabel, _sourceHint;
    private readonly CheckBox _colonCheck, _detailsCheck, _topMostCheck, _lockCheck, _autoStartCheck, _chimeCheck, _dateCheck, _calendarCheck;
    private readonly TextBox _credentialBox;
    private readonly Button _hourColor, _minuteColor, _weatherColor, _dateColor, _calendarColor;

    private static readonly string[] Cities =
    {
        "Adana", "Adıyaman", "Afyonkarahisar", "Ankara", "Antalya", "Aydın", "Balıkesir",
        "Bursa", "Çanakkale", "Denizli", "Diyarbakır", "Edirne", "Elazığ", "Erzurum",
        "Eskişehir", "Gaziantep", "Hatay", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş",
        "Kayseri", "Kocaeli", "Konya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla",
        "Muş", "Nevşehir", "Ordu", "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt",
        "Sivas", "Tekirdağ", "Tokat", "Trabzon", "Van", "Yalova", "Zonguldak"
    };

    private static readonly string[] Weights = { "Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold" };

    public SettingsWindow(AppSettings current)
    {
        _original = current;
        Result = Clone(current);

        var fonts = LoadFonts();
        _hourFont = CreateCombo(fonts, current.HourFontFamily);
        _minuteFont = CreateCombo(fonts, current.MinuteFontFamily);
        _dateFont = CreateCombo(fonts, current.DateFontFamily);
        _hourWeight = CreateCombo(Weights, current.HourFontWeight);
        _minuteWeight = CreateCombo(Weights, current.MinuteFontWeight);
        _dateWeight = CreateCombo(Weights, current.DateFontWeight);

        _sourceBox = new ComboBox { Width = 240 };
        AddTagged(_sourceBox, "Open-Meteo (anahtarsız)", "OpenMeteo");
        AddTagged(_sourceBox, "OpenWeather", "OpenWeather");
        AddTagged(_sourceBox, "WeatherAPI.com", "WeatherAPI");
        AddTagged(_sourceBox, "MET Norway", "MetNorway");

        _refreshBox = new ComboBox { Width = 240 };
        AddTagged(_refreshBox, "5 dakikada bir", "5");
        AddTagged(_refreshBox, "15 dakikada bir", "15");
        AddTagged(_refreshBox, "30 dakikada bir", "30");
        AddTagged(_refreshBox, "Saatte bir", "60");
        AddTagged(_refreshBox, "3 saatte bir", "180");

        _credentialBox = new TextBox { Width = 240 };
        _credentialLabel = new TextBlock { FontSize = 11, Foreground = UiTheme.Brush("#666666") };
        _sourceHint = new TextBlock { FontSize = 10, Foreground = UiTheme.Brush("#777777"), TextWrapping = TextWrapping.Wrap, MaxWidth = 360 };
        _credentialBoxHost = new ComboBox();

        _cityBox = CreateCombo(Cities, Cities.Contains(current.City) ? current.City : Cities[0]);

        _positionBox = new ComboBox { Width = 150 };
        AddTagged(_positionBox, "Saatin altında", "Below");
        AddTagged(_positionBox, "Saatin üstünde", "Above");
        _positionBox.SelectedIndex = current.WeatherPosition == "Above" ? 1 : 0;

        _datePositionBox = new ComboBox { Width = 150 };
        AddTagged(_datePositionBox, "Saatin üstünde", "Above");
        AddTagged(_datePositionBox, "Saatin altında", "Below");
        _datePositionBox.SelectedIndex = current.DatePosition == "Below" ? 1 : 0;

        _hourSize = CreateSlider(24, 400, 4, current.HourFontSize);
        _minuteSize = CreateSlider(24, 400, 4, current.MinuteFontSize);
        _weatherSize = CreateSlider(10, 60, 1, current.WeatherFontSize);
        _dateSize = CreateSlider(8, 80, 1, current.DateFontSize);
        _weatherOffsetX = CreateSlider(-200, 200, 4, current.WeatherOffsetX);
        _weatherOffsetY = CreateSlider(-200, 200, 4, current.WeatherOffsetY);
        _dateOffsetX = CreateSlider(-200, 200, 4, current.DateOffsetX);
        _dateOffsetY = CreateSlider(-200, 200, 4, current.DateOffsetY);

        _hourSizeLabel = new TextBlock();
        _minuteSizeLabel = new TextBlock();
        _weatherSizeLabel = new TextBlock();
        _dateSizeLabel = new TextBlock();
        _weatherOffsetXLabel = new TextBlock();
        _weatherOffsetYLabel = new TextBlock();
        _dateOffsetXLabel = new TextBlock();
        _dateOffsetYLabel = new TextBlock();

        _colonCheck = CreateCheck("Saat ve dakika arasına \":\" koy", current.ShowColon);
        _detailsCheck = CreateCheck("Detayları göster (hissedilen, nem, rüzgâr)", current.ShowWeatherDetails);
        _topMostCheck = CreateCheck("Widget her zaman üstte gösterilsin", current.TopMost);
        _lockCheck = CreateCheck("Konumu sabitle (sürükleme kapalı, konum hatırlanır)", current.LockPosition);
        _autoStartCheck = CreateCheck("Sistem açılınca otomatik başlat", current.StartWithWindows);
        _chimeCheck = CreateCheck("Saat başında uyarı sesi çal", current.HourlyChime);
        _dateCheck = CreateCheck("Tarihi göster (25.08.2026 - Salı)", current.ShowDate);
        _calendarCheck = CreateCheck("Widget'ta mini takvim göster", current.ShowCalendar);

        _hourColor = CreateColorButton(current.HourColor);
        _minuteColor = CreateColorButton(current.MinuteColor);
        _weatherColor = CreateColorButton(current.WeatherColor);
        _dateColor = CreateColorButton(current.DateColor);
        _calendarColor = CreateColorButton(current.CalendarColor);

        _hourColor.Click += (_, _) => PickColor(_hourColor);
        _minuteColor.Click += (_, _) => PickColor(_minuteColor);
        _weatherColor.Click += (_, _) => PickColor(_weatherColor);
        _dateColor.Click += (_, _) => PickColor(_dateColor);
        _calendarColor.Click += (_, _) => PickColor(_calendarColor);

        _soundBox = new ComboBox { Width = 280 };
        LoadSounds(current.ChimeSoundFile);

        _credentialDrafts["OpenWeather"] = current.OpenWeatherApiKey;
        _credentialDrafts["WeatherAPI"] = current.WeatherApiKey;
        _credentialDrafts["MetNorway"] = current.MetNorwayUserAgent;
        _activeCredentialSource = string.IsNullOrWhiteSpace(current.WeatherSource) ? "OpenMeteo" : current.WeatherSource;
        SelectTagged(_sourceBox, _activeCredentialSource, "OpenMeteo");
        int refresh = new[] { 5, 15, 30, 60, 180 }.Contains(current.WeatherRefreshMinutes) ? current.WeatherRefreshMinutes : 15;
        SelectTagged(_refreshBox, refresh.ToString(), "15");
        _credentialBox.Text = _credentialDrafts.GetValueOrDefault(_activeCredentialSource, "");

        foreach (var slider in new[] { _hourSize, _minuteSize, _weatherSize, _dateSize, _weatherOffsetX, _weatherOffsetY, _dateOffsetX, _dateOffsetY })
        {
            slider.ValueChanged += (_, _) => UpdateLabels();
        }
        _sourceBox.SelectionChanged += (_, _) => SourceChanged();
        UpdateLabels();
        UpdateSourceControls();
        _controlsReady = true;

        var content = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        content.Children.Add(TimeGroup());
        content.Children.Add(WeatherGroup());
        content.Children.Add(DateGroup());
        content.Children.Add(GeneralGroup());
        content.Children.Add(ButtonRow());

        Title = "Widget Ayarları";
        Width = 580;
        SizeToContent = SizeToContent.Height;
        MaxHeight = Math.Max(300, Screens.Primary?.WorkingArea.Height - 40 ?? 700);
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Icon = App.LoadWindowIcon();
        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = content
        };
    }

    private static List<string> LoadFonts()
    {
        try
        {
            return FontManager.Current.SystemFonts.Select(f => f.Name).Distinct().OrderBy(n => n).ToList();
        }
        catch
        {
            return new List<string> { "DejaVu Sans", "Liberation Sans", "Arial", "Segoe UI", "Consolas" };
        }
    }

    private static ComboBox CreateCombo(IEnumerable<string> items, string selected)
    {
        var box = new ComboBox { Width = 200 };
        foreach (var item in items)
        {
            box.Items.Add(item);
        }
        box.SelectedItem = box.Items.Contains(selected) ? selected : box.Items[0];
        return box;
    }

    private static ComboBoxItem AddTagged(ComboBox box, string content, string tag)
    {
        var item = new ComboBoxItem { Content = content, Tag = tag };
        box.Items.Add(item);
        return item;
    }

    private static void SelectTagged(ComboBox box, string tag, string fallback)
    {
        var item = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, tag))
                   ?? box.Items.OfType<ComboBoxItem>().First(i => Equals(i.Tag, fallback));
        box.SelectedItem = item;
    }

    private static string? TagOf(ComboBox box) => (box.SelectedItem as ComboBoxItem)?.Tag as string;

    private static Slider CreateSlider(double min, double max, double tick, double value)
        => new()
        {
            Minimum = min,
            Maximum = max,
            TickFrequency = tick,
            IsSnapToTickEnabled = true,
            Value = Math.Clamp(value, min, max),
            Width = 220
        };

    private static CheckBox CreateCheck(string text, bool value)
        => new() { Content = text, IsChecked = value, FontSize = 12 };

    private static Button CreateColorButton(string hex)
        => new()
        {
            Tag = hex,
            Background = UiTheme.Brush(hex),
            Height = 26,
            Width = 200,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

    private async void PickColor(Button button)
    {
        var picker = new ColorPickerWindow(button.Tag as string ?? "#1F1E1D");
        var result = await picker.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(result)) return;
        button.Tag = result;
        button.Background = UiTheme.Brush(result);
        PreviewRequested?.Invoke(BuildResult());
    }

    private void UpdateLabels()
    {
        _hourSizeLabel.Text = $"{_hourSize.Value:0} pt";
        _minuteSizeLabel.Text = $"{_minuteSize.Value:0} pt";
        _weatherSizeLabel.Text = $"{_weatherSize.Value:0} pt";
        _dateSizeLabel.Text = $"{_dateSize.Value:0} pt";
        _weatherOffsetXLabel.Text = $"{_weatherOffsetX.Value:0} px";
        _weatherOffsetYLabel.Text = $"{_weatherOffsetY.Value:0} px";
        _dateOffsetXLabel.Text = $"{_dateOffsetX.Value:0} px";
        _dateOffsetYLabel.Text = $"{_dateOffsetY.Value:0} px";
    }

    private void SourceChanged()
    {
        if (!_controlsReady) return;
        _credentialDrafts[_activeCredentialSource] = _credentialBox.Text ?? "";
        _activeCredentialSource = TagOf(_sourceBox) ?? "OpenMeteo";
        _credentialBox.Text = _credentialDrafts.GetValueOrDefault(_activeCredentialSource, "");
        UpdateSourceControls();
    }

    private void UpdateSourceControls()
    {
        var openMeteo = _activeCredentialSource == "OpenMeteo";
        _credentialLabel.IsVisible = !openMeteo;
        _credentialBox.IsVisible = !openMeteo;
        _refreshBox.IsEnabled = _activeCredentialSource != "MetNorway";

        switch (_activeCredentialSource)
        {
            case "OpenWeather":
                _credentialLabel.Text = "OpenWeather API anahtarı";
                _sourceHint.Text = "API anahtarı gerekir. Anahtar settings.json dosyasında saklanır.";
                break;
            case "WeatherAPI":
                _credentialLabel.Text = "WeatherAPI.com API anahtarı";
                _sourceHint.Text = "API anahtarı gerekir. Anahtar settings.json dosyasında saklanır.";
                break;
            case "MetNorway":
                _credentialLabel.Text = "MET Norway User-Agent";
                _sourceHint.Text = "Türkiye için aynı konum isteği 3 saatten sık yinelenmemelidir. Örnek: WindowsClockWidget/1.2.5 (eposta adresiniz).";
                SelectTagged(_refreshBox, "180", "180");
                break;
            default:
                _sourceHint.Text = "API anahtarı gerekmez. Şehir koordinatları Open-Meteo Geocoding ile bulunur.";
                break;
        }
    }

    private void LoadSounds(string selected)
    {
        try
        {
            var files = SoundPlayer.SoundFiles();
            foreach (var file in files.OrderBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase))
            {
                AddTagged(_soundBox, Path.GetFileName(file), file);
            }
            var match = _soundBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, selected, StringComparison.OrdinalIgnoreCase))
                ?? _soundBox.Items.OfType<ComboBoxItem>()
                    .FirstOrDefault(i => "chimes.wav".Equals(i.Content as string, StringComparison.OrdinalIgnoreCase));
            _soundBox.SelectedItem = match ?? _soundBox.Items.OfType<ComboBoxItem>().FirstOrDefault();
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }

    private async void PreviewSound(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var path = TagOf(_soundBox);
            if (string.IsNullOrWhiteSpace(path)) return;
            using var player = new SoundPlayer(path);
            await Task.Run(player.PlaySync);
        }
        catch (Exception ex)
        {
            App.Log(ex);
            MessageBox.Show($"Ses çalınamadı: {ex.Message}\n\nAyrıntılar: {App.LogPath}",
                "WindowsClockWidget - Ses", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private Border Group(string title, params Control[] children)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = UiTheme.Brush("#1F1E1D"),
            Margin = new Thickness(0, 0, 0, 4)
        });
        foreach (var child in children)
        {
            stack.Children.Add(child);
        }
        return new Border
        {
            Background = UiTheme.Brush("#FFFFFF"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Child = stack
        };
    }

    private static TextBlock Field(string text) => new()
    {
        Text = text,
        FontSize = 11,
        Foreground = UiTheme.Brush("#666666")
    };

    private static StackPanel Row(params Control[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var child in children)
        {
            panel.Children.Add(child);
        }
        return panel;
    }

    private Border TimeGroup()
    {
        var hourColumn = new StackPanel
        {
            Children = { Field("Saat yazı tipi"), _hourFont, Field("Kalınlık"), _hourWeight, Field("Boyut"), _hourSize, _hourSizeLabel, Field("Renk"), _hourColor }
        };
        var minuteColumn = new StackPanel
        {
            Children = { Field("Dakika yazı tipi"), _minuteFont, Field("Kalınlık"), _minuteWeight, Field("Boyut"), _minuteSize, _minuteSizeLabel, Field("Renk"), _minuteColor }
        };
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,20,*"),
            Children = { hourColumn, minuteColumn }
        };
        Grid.SetColumn(minuteColumn, 2);
        return Group("SAAT / DAKİKA", grid);
    }

    private Border WeatherGroup() => Group("HAVA DURUMU",
        Field("Kaynak"), _sourceBox,
        Field("Güncelleme sıklığı"), _refreshBox,
        _sourceHint,
        Field("API anahtarı / User-Agent"), _credentialBox,
        Field("Sıcaklık boyutu"), _weatherSize, _weatherSizeLabel,
        _detailsCheck, _colonCheck,
        Row(new StackPanel { Children = { Field("Renk"), _weatherColor } }, new StackPanel { Children = { Field("Konum"), _positionBox } }),
        Field("Şehir"), _cityBox,
        Row(new StackPanel { Children = { Field("Yatay (X)"), _weatherOffsetX, _weatherOffsetXLabel } },
            new StackPanel { Children = { Field("Dikey (Y)"), _weatherOffsetY, _weatherOffsetYLabel } }));

    private Border DateGroup() => Group("TARİH",
        _dateCheck, _calendarCheck,
        Row(Field("Takvim rengi"), _calendarColor),
        Field("Yazı tipi"), _dateFont,
        Field("Kalınlık"), _dateWeight,
        Field("Boyut"), _dateSize, _dateSizeLabel,
        Row(new StackPanel { Children = { Field("Renk"), _dateColor } }, new StackPanel { Children = { Field("Konum"), _datePositionBox } }),
        Row(new StackPanel { Children = { Field("Yatay (X)"), _dateOffsetX, _dateOffsetXLabel } },
            new StackPanel { Children = { Field("Dikey (Y)"), _dateOffsetY, _dateOffsetYLabel } }));

    private Border GeneralGroup() => Group("GENEL",
        _topMostCheck, _lockCheck, _autoStartCheck, _chimeCheck,
        Row(_soundBox, Button("Dinle", PreviewSound, 70)));

    private StackPanel ButtonRow() => new()
    {
        Orientation = Orientation.Horizontal,
        HorizontalAlignment = HorizontalAlignment.Right,
        Spacing = 10,
        Children =
        {
            Button("Uygula", Apply_Click, 100, UiTheme.Brush("#3498DB")),
            Button("Kaydet", Save_Click, 100, UiTheme.Brush("#1F1E1D")),
            Button("Vazgeç", Cancel_Click, 100, UiTheme.Brush("#E4E4E6"))
        }
    };

    private static Button Button(string text, EventHandler<Avalonia.Interactivity.RoutedEventArgs> handler, double width = 90, IBrush? background = null)
    {
        var button = new Button
        {
            Content = text,
            Width = width,
            Height = 32,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Background = background ?? UiTheme.Brush("#E4E4E6"),
            Foreground = background is null ? UiTheme.Brush("#1F1E1D") : Brushes.White,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        button.Click += handler;
        return button;
    }

    private static AppSettings Clone(AppSettings s) => new()
    {
        City = s.City,
        HourFontFamily = s.HourFontFamily, HourFontSize = s.HourFontSize,
        HourFontWeight = s.HourFontWeight, HourColor = s.HourColor,
        MinuteFontFamily = s.MinuteFontFamily, MinuteFontSize = s.MinuteFontSize,
        MinuteFontWeight = s.MinuteFontWeight, MinuteColor = s.MinuteColor,
        TextColor = s.TextColor, ShowColon = s.ShowColon,
        WeatherFontSize = s.WeatherFontSize, WeatherColor = s.WeatherColor,
        WeatherPosition = s.WeatherPosition, WeatherAlign = s.WeatherAlign,
        ShowWeatherDetails = s.ShowWeatherDetails,
        TopMost = s.TopMost, LockPosition = s.LockPosition,
        PosX = s.PosX, PosY = s.PosY,
        WeatherRefreshMinutes = s.WeatherRefreshMinutes,
        StartWithWindows = s.StartWithWindows,
        HourlyChime = s.HourlyChime, ChimeSoundFile = s.ChimeSoundFile,
        ShowCalendar = s.ShowCalendar, CalendarColor = s.CalendarColor,
        ShowDate = s.ShowDate, DateFontFamily = s.DateFontFamily,
        DateFontSize = s.DateFontSize, DateFontWeight = s.DateFontWeight,
        DateColor = s.DateColor, DatePosition = s.DatePosition,
        DateOffsetX = s.DateOffsetX, DateOffsetY = s.DateOffsetY,
        WeatherOffsetX = s.WeatherOffsetX, WeatherOffsetY = s.WeatherOffsetY,
        WeatherSource = s.WeatherSource,
        OpenWeatherApiKey = s.OpenWeatherApiKey,
        WeatherApiKey = s.WeatherApiKey,
        MetNorwayUserAgent = s.MetNorwayUserAgent
    };

    private AppSettings BuildResult()
    {
        var r = Clone(_original);
        r.HourFontFamily = _hourFont.SelectedItem as string ?? "Segoe UI";
        r.HourFontWeight = _hourWeight.SelectedItem as string ?? "Light";
        r.HourFontSize = _hourSize.Value;
        r.HourColor = _hourColor.Tag as string ?? "#1F1E1D";
        r.MinuteFontFamily = _minuteFont.SelectedItem as string ?? "Segoe UI";
        r.MinuteFontWeight = _minuteWeight.SelectedItem as string ?? "Light";
        r.MinuteFontSize = _minuteSize.Value;
        r.MinuteColor = _minuteColor.Tag as string ?? "#1F1E1D";
        r.WeatherFontSize = _weatherSize.Value;
        r.WeatherColor = _weatherColor.Tag as string ?? "#1F1E1D";
        r.ShowColon = _colonCheck.IsChecked == true;
        r.ShowWeatherDetails = _detailsCheck.IsChecked == true;
        r.TopMost = _topMostCheck.IsChecked == true;
        r.LockPosition = _lockCheck.IsChecked == true;
        r.StartWithWindows = _autoStartCheck.IsChecked == true;
        r.HourlyChime = _chimeCheck.IsChecked == true;
        r.ChimeSoundFile = TagOf(_soundBox) ?? "";
        r.ShowCalendar = _calendarCheck.IsChecked == true;
        r.CalendarColor = _calendarColor.Tag as string ?? "#1F1E1D";
        r.ShowDate = _dateCheck.IsChecked == true;
        r.DateFontFamily = _dateFont.SelectedItem as string ?? "Segoe UI";
        r.DateFontWeight = _dateWeight.SelectedItem as string ?? "Normal";
        r.DateFontSize = _dateSize.Value;
        r.DateColor = _dateColor.Tag as string ?? "#1F1E1D";
        r.DatePosition = TagOf(_datePositionBox) == "Below" ? "Below" : "Above";
        r.DateOffsetX = _dateOffsetX.Value;
        r.DateOffsetY = _dateOffsetY.Value;
        r.WeatherOffsetX = _weatherOffsetX.Value;
        r.WeatherOffsetY = _weatherOffsetY.Value;
        r.WeatherPosition = TagOf(_positionBox) == "Above" ? "Above" : "Below";
        r.City = _cityBox.SelectedItem as string ?? Cities[0];
        r.WeatherSource = TagOf(_sourceBox) ?? "OpenMeteo";
        _credentialDrafts[_activeCredentialSource] = _credentialBox.Text?.Trim() ?? "";
        r.OpenWeatherApiKey = _credentialDrafts.GetValueOrDefault("OpenWeather", "");
        r.WeatherApiKey = _credentialDrafts.GetValueOrDefault("WeatherAPI", "");
        r.MetNorwayUserAgent = _credentialDrafts.GetValueOrDefault("MetNorway", "");
        r.WeatherRefreshMinutes = int.TryParse(TagOf(_refreshBox), out var interval) ? interval : 15;
        return r;
    }

    private void Apply_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = BuildResult();
        ApplyRequested?.Invoke(Result);
    }

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = BuildResult();
        Accepted = true;
        Close();
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Accepted = false;
        Close();
    }
}

internal sealed class ColorPickerWindow : Window
{
    private readonly ColorView _picker;

    public ColorPickerWindow(string current)
    {
        Color initial;
        try { initial = UiTheme.Brush(current).Color; }
        catch { initial = Color.FromRgb(0x1F, 0x1E, 0x1D); }

        _picker = new ColorView
        {
            Color = initial,
            IsAlphaEnabled = false,
            IsAlphaVisible = false,
            Width = 380
        };

        var ok = new Button { Content = "Tamam", Width = 90, Margin = new Thickness(0, 0, 8, 0) };
        ok.Click += (_, _) => Close($"#{_picker.Color.R:X2}{_picker.Color.G:X2}{_picker.Color.B:X2}");
        var cancel = new Button { Content = "İptal", Width = 90 };
        cancel.Click += (_, _) => Close(null);

        Title = "Renk seç";
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Icon = App.LoadWindowIcon();
        Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                _picker,
                Row(ok, cancel)
            }
        };
    }

    private static StackPanel Row(params Control[] children)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (var child in children) panel.Children.Add(child);
        return panel;
    }
}
