using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using System.Globalization;

namespace WindowsClockWidget;

public class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _weatherTimer;
    private readonly DispatcherTimer _reminderTimer;
    private DispatcherTimer? _notificationTimer;
    private readonly WeatherService _weather = new();
    private readonly ReminderService _reminderService = new();
    private AppSettings _settings;
    private WeatherInfo? _lastWeather;
    private bool _weatherBusy;
    private int _lastChimeHour = -1;
    private int _calYear, _calMonth;
    private List<Reminder> _reminders = new();
    private SoundPlayer? _chimePlayer;
    private SoundPlayer? _reminderPlayer;
    private TrayIcon? _trayIcon;
    private bool _soundErrorShown;

    private readonly TextBlock _hourText;
    private readonly TextBlock _colonText;
    private readonly TextBlock _minuteText;
    private readonly TextBlock _dateText;
    private readonly StackPanel _clockRow;
    private readonly StackPanel _weatherPanel;
    private readonly TextBlock _weatherIcon;
    private readonly TextBlock _tempText;
    private readonly TextBlock _descText;
    private readonly TextBlock _detailsText;
    private readonly Border _calendarPanel;
    private readonly TextBlock _calMonthLabel;
    private readonly TextBlock _calPrev;
    private readonly TextBlock _calNext;
    private readonly Grid _calHeaders;
    private readonly Grid _calDays;
    private readonly TextBlock _reminderDetails;
    private readonly ContextMenu _contextMenu;

    public MainWindow()
    {
        _hourText = new TextBlock { Text = "00", VerticalAlignment = VerticalAlignment.Top };
        _colonText = new TextBlock { Text = ":", VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(3, 0) };
        _minuteText = new TextBlock { Text = "00", VerticalAlignment = VerticalAlignment.Top };
        StabilizeText(_hourText);
        StabilizeText(_colonText);
        StabilizeText(_minuteText);
        _clockRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Top,
            Children = { _hourText, _colonText, _minuteText }
        };

        _dateText = new TextBlock { FontSize = 14, Margin = new Thickness(0, 2, 0, 0), IsVisible = false };
        StabilizeText(_dateText);

        _weatherIcon = new TextBlock { Text = "☁", FontSize = 28, VerticalAlignment = VerticalAlignment.Center };
        _tempText = new TextBlock { Text = "--°", FontSize = 20, FontWeight = FontWeight.SemiBold };
        _descText = new TextBlock { Text = "yükleniyor…", FontSize = 11, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxWidth = 280 };
        _detailsText = new TextBlock { FontSize = 10, IsVisible = false };
        StabilizeText(_weatherIcon);
        StabilizeText(_tempText);
        StabilizeText(_descText);
        StabilizeText(_detailsText);
        var weatherInner = new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Children = { _tempText, _descText }
                },
                _detailsText
            }
        };
        _weatherPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Children = { _weatherIcon, weatherInner }
        };

        _calMonthLabel = new TextBlock { FontSize = 11, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _calPrev = new TextBlock { Text = "◀", FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0), Opacity = 0.5, Cursor = Cursor.Default };
        _calNext = new TextBlock { Text = "▶", FontSize = 10, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Opacity = 0.5, Cursor = Cursor.Default };
        _calHeaders = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        _calDays = new Grid();
        _reminderDetails = new TextBlock { FontSize = 9, Opacity = 0.55, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0), MaxWidth = 200 };
        _calendarPanel = new Border
        {
            Margin = new Thickness(0, 6, 0, 0),
            IsVisible = false,
            Child = new StackPanel
            {
                Children =
                {
                    new Grid
                    {
                        Margin = new Thickness(0, 0, 0, 4),
                        ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                        Children =
                        {
                            _calPrev,
                            _calMonthLabel,
                            _calNext
                        }
                    },
                    _calHeaders,
                    _calDays,
                    _reminderDetails
                }
            }
        };
        Grid.SetColumn(_calMonthLabel, 1);
        Grid.SetColumn(_calNext, 2);

        var dock = new DockPanel { LastChildFill = true };
        dock.Children.Add(_dateText);
        dock.Children.Add(_calendarPanel);
        dock.Children.Add(_weatherPanel);
        dock.Children.Add(_clockRow);

        var root = new Border
        {
            Margin = new Thickness(10),
            Padding = new Thickness(22, 10, 22, 12),
            Background = Brushes.Transparent,
            Child = dock
        };
        var container = new Grid { Children = { root } };

        _contextMenu = new ContextMenu();
        AddMenuItem("Ayarlar", Settings_Click);
        AddMenuItem("Hava Durumunu Güncelle", WeatherRefresh_Click);
        AddMenuItem("Takvim", Calendar_Click);
        AddMenuItem("Kapat", Close_Click);
        AddMenuItem($"xpow3r 2026  •  v{AppSettings.AppVersion}", null);
        container.ContextMenu = _contextMenu;

        container.PointerPressed += Root_PointerPressed;
        _calPrev.PointerPressed += (_, e) => { ShiftMonth(-1); e.Handled = true; };
        _calNext.PointerPressed += (_, e) => { ShiftMonth(1); e.Handled = true; };

        Title = "Saat";
        Content = container;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowDecorations = WindowDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Icon = App.LoadWindowIcon();

        _settings = AppSettings.Load();
        InitCalendar();
        ApplySettings();

        if (!double.IsNaN(_settings.PosX) && !double.IsNaN(_settings.PosY))
        {
            Position = new PixelPoint((int)_settings.PosX, (int)_settings.PosY);
        }
        else
        {
            var screen = Screens.Primary?.WorkingArea ?? new PixelRect(0, 0, 800, 600);
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint(screen.X + screen.Width / 2 - 150, screen.Y + screen.Height / 2 - 100);
        }

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _weatherTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(Math.Max(1, _settings.WeatherRefreshMinutes)) };
        _weatherTimer.Tick += async (_, _) => await LoadWeatherAsync();
        _weatherTimer.Start();

        _reminderTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _reminderTimer.Start();

        UpdateClock();
        _lastChimeHour = DateTime.Now.Hour;
        _ = LoadWeatherAsync();
        CheckReminders();
        SetupTray();
        Closed += (_, _) => OnClosed();
    }

    private void AddMenuItem(string header, EventHandler<RoutedEventArgs>? handler)
    {
        var item = new MenuItem { Header = header };
        if (handler is not null)
        {
            item.Click += handler;
        }
        else
        {
            item.IsEnabled = false;
        }
        _contextMenu.Items.Add(item);
    }

    private void Root_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);
        if (point.Properties.IsRightButtonPressed)
        {
            _contextMenu.Open(sender as Control ?? this);
            e.Handled = true;
            return;
        }
        if (point.Properties.IsLeftButtonPressed && !_settings.LockPosition)
        {
            BeginMoveDrag(e);
            e.Handled = true;
        }
    }

    private void SetupTray()
    {
        try
        {
            _trayIcon = new TrayIcon
            {
                Icon = App.LoadWindowIcon(),
                ToolTipText = "WindowsClockWidget",
                IsVisible = true
            };
            var menu = new NativeMenu();
            AddTrayItem(menu, "Ayarlar", (_, _) => Settings_Click(null, new RoutedEventArgs()));
            AddTrayItem(menu, "Hava Durumunu Güncelle", async (_, _) => await LoadWeatherAsync());
            AddTrayItem(menu, "Takvim", (_, _) => Calendar_Click(null, new RoutedEventArgs()));
            AddTrayItem(menu, "Kapat", (_, _) => Close());
            _trayIcon.Menu = menu;
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }

    private static void AddTrayItem(NativeMenu menu, string header, EventHandler handler)
    {
        var item = new NativeMenuItem(header);
        item.Click += handler;
        menu.Items.Add(item);
    }

    private void OnClosed()
    {
        if (_settings.LockPosition)
        {
            _settings.PosX = Position.X;
            _settings.PosY = Position.Y;
            _settings.Save();
        }
        _clockTimer.Stop();
        _weatherTimer.Stop();
        _reminderTimer.Stop();
        _notificationTimer?.Stop();
        _chimePlayer?.Dispose();
        _reminderPlayer?.Dispose();
        _trayIcon?.Dispose();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        _hourText.Text = now.ToString("HH", CultureInfo.InvariantCulture);
        _minuteText.Text = now.ToString("mm", CultureInfo.InvariantCulture);
        AlignClockText();

        if (_settings.ShowDate && _notificationTimer is not { IsEnabled: true })
        {
            _dateText.Text = now.ToString("dd.MM.yyyy - dddd", new CultureInfo("tr-TR"));
        }

        if (_settings.HourlyChime && now.Minute == 0 && now.Second < 5 && _lastChimeHour != now.Hour)
        {
            _lastChimeHour = now.Hour;
            PlayChime();
        }
    }

    private void PlayChime()
    {
        try
        {
            var path = _settings.ChimeSoundFile;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                path = SoundPlayer.DefaultChimePath;
            }
            if (string.IsNullOrWhiteSpace(path)) return;
            _chimePlayer?.Dispose();
            _chimePlayer = new SoundPlayer(path);
            _chimePlayer.Load();
            _chimePlayer.Play();
        }
        catch (Exception ex)
        {
            ReportSoundError(ex);
        }
    }

    private async Task LoadWeatherAsync()
    {
        if (_weatherBusy) return;
        _weatherBusy = true;
        try
        {
            _descText.Opacity = 0.5;
            _descText.Text = "yükleniyor…";
            var info = await _weather.GetWeatherAsync(_settings);
            _lastWeather = info;
            _tempText.Text = $"{info.Temperature:0}°";
            _descText.Text = $"{info.Description} - {_settings.City}";
            _descText.Opacity = 0.75;
            _weatherIcon.Text = (info.Icon ?? WeatherService.IconFor(info.Code)).ToString();
            ToolTip.SetTip(this, $"Hissedilen {info.Apparent:0}° • Nem %{info.Humidity} • Rüzgâr {info.WindSpeed:0} km/s");
        }
        catch (Exception ex)
        {
            _lastWeather = null;
            _tempText.Text = "--°";
            _weatherIcon.Text = "!";
            _descText.Text = GetWeatherErrorText(ex);
            _descText.Opacity = 0.5;
            if (ex is not WeatherRateLimitException { IsRetrySuppressed: true })
            {
                App.Log(ex);
            }
            ToolTip.SetTip(this, $"{_descText.Text}\nAyrıntı: {ex.Message}");
        }
        finally
        {
            _weatherBusy = false;
            UpdateDetails();
        }
    }

    private static string GetWeatherErrorText(Exception ex) => ex switch
    {
        WeatherRateLimitException limit => $"{limit.Provider} güncelleme sınırına ulaşıldı. {limit.RetryAt.ToLocalTime():HH:mm} sonrasında yeniden deneyin.",
        TimeoutException or TaskCanceledException => "Hava durumu isteği zaman aşımına uğradı.",
        HttpRequestException => "Hava durumu sunucusuna ulaşılamadı.",
        System.Text.Json.JsonException => "Hava durumu sunucusundan geçersiz yanıt alındı.",
        _ => ex.Message
    };

    private async void WeatherRefresh_Click(object? sender, RoutedEventArgs e)
    {
        await LoadWeatherAsync();
    }

    private void UpdateDetails()
    {
        if (_settings.ShowWeatherDetails && _lastWeather is { } info)
        {
            _detailsText.Text = $"Hissedilen {info.Apparent:0}°   •   Nem %{info.Humidity}   •   Rüzgâr {info.WindSpeed:0} km/s";
            _detailsText.IsVisible = true;
        }
        else
        {
            _detailsText.IsVisible = false;
        }
    }

    public void ApplySettings()
    {
        _colonText.IsVisible = _settings.ShowColon;
        ApplyPart(_hourText, _settings.HourFontFamily, _settings.HourFontSize, _settings.HourFontWeight, _settings.HourColor);
        ApplyPart(_minuteText, _settings.MinuteFontFamily, _settings.MinuteFontSize, _settings.MinuteFontWeight, _settings.MinuteColor);
        ApplyPart(_colonText, _settings.MinuteFontFamily, _settings.MinuteFontSize, _settings.MinuteFontWeight, _settings.MinuteColor);

        double hs = _settings.HourFontSize;
        double ms = _settings.MinuteFontSize;
        double topSpace = 0;
        double bottomSpace = 0;
        AlignClockText();
        try
        {
            var (_, lineH, inkTopH) = MeasureInk(_settings.HourFontFamily, hs, _settings.HourFontWeight, "00");
            var (_, lineM, inkTopM) = MeasureInk(_settings.MinuteFontFamily, ms, _settings.MinuteFontWeight, "00");
            topSpace = Math.Min(inkTopH, inkTopM);
            bottomSpace = Math.Max(lineH - inkTopH, lineM - inkTopM);
        }
        catch
        {
        }
        _clockRow.Margin = new Thickness(0, -Math.Round(topSpace), 0, 0);

        _dateText.IsVisible = _settings.ShowDate;
        if (_settings.ShowDate)
        {
            _dateText.FontFamily = new FontFamily(_settings.DateFontFamily);
            _dateText.FontSize = _settings.DateFontSize;
            _dateText.FontWeight = UiTheme.Weight(_settings.DateFontWeight);
            _dateText.Foreground = UiTheme.Brush(_settings.DateColor);
            DockPanel.SetDock(_dateText, _settings.DatePosition == "Above" ? Dock.Top : Dock.Bottom);
            _dateText.Margin = _settings.DatePosition == "Above"
                ? new Thickness(0, 2, 0, 0)
                : new Thickness(0, 0, 0, 2);
            _dateText.RenderTransform = new TranslateTransform(Math.Round(_settings.DateOffsetX), -Math.Round(_settings.DateOffsetY));
        }

        Topmost = _settings.TopMost;

        bool above = _settings.WeatherPosition == "Above";
        DockPanel.SetDock(_weatherPanel, above ? Dock.Top : Dock.Bottom);
        _weatherPanel.Margin = above
            ? new Thickness(0, 0, 0, 4)
            : new Thickness(0, -Math.Round(Math.Min(bottomSpace, 180)), 0, 0);
        _weatherPanel.RenderTransform = new TranslateTransform(Math.Round(_settings.WeatherOffsetX), -Math.Round(_settings.WeatherOffsetY));

        var weatherBrush = UiTheme.Brush(_settings.WeatherColor);
        _tempText.FontSize = _settings.WeatherFontSize;
        _tempText.Foreground = weatherBrush;
        _descText.FontSize = Math.Max(9, _settings.WeatherFontSize * 0.55);
        _descText.Foreground = weatherBrush;
        _detailsText.FontSize = Math.Max(9, _settings.WeatherFontSize * 0.5);
        _detailsText.Foreground = weatherBrush;
        _weatherIcon.FontSize = _settings.WeatherFontSize * 1.4;
        _weatherIcon.Foreground = weatherBrush;

        _calendarPanel.IsVisible = _settings.ShowCalendar;
        if (_settings.ShowCalendar)
        {
            _reminders = _reminderService.Load();
            BuildCalendarGrid();
            RefreshCalendar();
        }

        UpdateDetails();
    }

    private static void StabilizeText(Control control)
    {
        control.UseLayoutRounding = true;
        TextOptions.SetTextRenderingMode(control, TextRenderingMode.Antialias);
        TextOptions.SetBaselinePixelAlignment(control, BaselinePixelAlignment.Aligned);
    }

    private static void ApplyPart(TextBlock block, string family, double size, string weight, string color)
    {
        block.FontFamily = new FontFamily(family);
        block.FontSize = size;
        block.FontWeight = UiTheme.Weight(weight);
        block.Foreground = UiTheme.Brush(color);
    }

    private void AlignClockText()
    {
        try
        {
            var (_, _, inkH) = MeasureInk(_settings.HourFontFamily, _settings.HourFontSize, _settings.HourFontWeight,
                string.IsNullOrEmpty(_hourText.Text) ? "00" : _hourText.Text);
            var (_, _, inkM) = MeasureInk(_settings.MinuteFontFamily, _settings.MinuteFontSize, _settings.MinuteFontWeight,
                string.IsNullOrEmpty(_minuteText.Text) ? "00" : _minuteText.Text);
            double shiftHour = Math.Round(Math.Max(0, inkM - inkH));
            double shiftMinute = Math.Round(Math.Max(0, inkH - inkM));
            _hourText.RenderTransform = new TranslateTransform(0, shiftHour);
            _colonText.RenderTransform = new TranslateTransform(0, shiftMinute);
            _minuteText.RenderTransform = new TranslateTransform(0, shiftMinute);
        }
        catch
        {
            _hourText.RenderTransform = new TranslateTransform(0, 0);
            _colonText.RenderTransform = new TranslateTransform(0, 0);
            _minuteText.RenderTransform = new TranslateTransform(0, 0);
        }
    }

    private static (double Baseline, double Height, double InkTop) MeasureInk(string family, double size, string weight, string text)
    {
        var typeface = new Typeface(new FontFamily(family), FontStyle.Normal, UiTheme.Weight(weight), FontStretch.Normal);
        var layout = new TextLayout(text, typeface, size, Brushes.Transparent);
        var line = layout.TextLines[0];
        var inkBoundsProperty = line.GetType().GetProperty("InkBounds",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (inkBoundsProperty?.GetValue(line) is Rect inkBounds && inkBounds.Height > 0)
        {
            return (line.Baseline, line.Height, inkBounds.Top);
        }
        var glyphTypeface = typeface.GlyphTypeface;
        var fontMetrics = glyphTypeface.Metrics;
        glyphTypeface.CharacterToGlyphMap.TryGetGlyph('0', out var glyph);
        var inkTop = line.Baseline;
        if (fontMetrics.DesignEmHeight > 0 && glyphTypeface.TryGetGlyphMetrics(glyph, out var glyphMetrics))
        {
            inkTop -= glyphMetrics.YBearing * size / fontMetrics.DesignEmHeight;
        }
        return (line.Baseline, line.Height, inkTop);
    }

    private void ApplyNewSettings(AppSettings settings)
    {
        settings.PosX = Position.X;
        settings.PosY = Position.Y;
        _settings = settings;
        _settings.Save();
        ApplySettings();
        StartupManager.Set(_settings.StartWithWindows);
        _weatherTimer.Interval = TimeSpan.FromMinutes(Math.Max(1, _settings.WeatherRefreshMinutes));
        _weatherTimer.Start();
        _ = LoadWeatherAsync();
    }

    private void PreviewSettings(AppSettings settings)
    {
        settings.PosX = Position.X;
        settings.PosY = Position.Y;
        _settings = settings;
        ApplySettings();
    }

    private async void Settings_Click(object? sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_settings)
        {
            PreviewRequested = PreviewSettings,
            ApplyRequested = ApplyNewSettings
        };
        await window.ShowDialog(this);
        if (window.Accepted)
        {
            ApplyNewSettings(window.Result);
        }
    }

    private async void Calendar_Click(object? sender, RoutedEventArgs e)
    {
        var window = new CalendarWindow(DateTime.Today);
        await window.ShowDialog(this);
        _reminders = _reminderService.Load();
        if (_settings.ShowCalendar)
        {
            RefreshCalendar();
        }
    }

    private void Close_Click(object? sender, EventArgs e) => Close();

    private void InitCalendar()
    {
        var now = DateTime.Now;
        _calYear = now.Year;
        _calMonth = now.Month;
        _reminders = _reminderService.Load();
        BuildCalendarGrid();
        RefreshCalendar();
    }

    private void BuildCalendarGrid()
    {
        var dim = UiTheme.Brush(_settings.CalendarColor, 0.45);
        _calHeaders.Children.Clear();
        _calHeaders.ColumnDefinitions.Clear();
        string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        for (int c = 0; c < 7; c++)
        {
            _calHeaders.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            var label = new TextBlock
            {
                Text = days[c],
                FontSize = 8,
                FontWeight = FontWeight.SemiBold,
                Foreground = dim,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(label, c);
            _calHeaders.Children.Add(label);
        }

        _calDays.RowDefinitions.Clear();
        _calDays.ColumnDefinitions.Clear();
        _calDays.Children.Clear();
        for (int r = 0; r < 6; r++)
        {
            _calDays.RowDefinitions.Add(new RowDefinition(18, GridUnitType.Pixel));
        }
        for (int c = 0; c < 7; c++)
        {
            _calDays.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }
    }

    private void RefreshCalendar()
    {
        var culture = new CultureInfo("tr-TR");
        _calMonthLabel.Text = culture.DateTimeFormat.MonthNames[_calMonth - 1].ToUpper() + " " + _calYear;

        var brush = UiTheme.Brush(_settings.CalendarColor);
        var todayBrush = UiTheme.Brush(_settings.CalendarColor, 0.18);
        var dim = UiTheme.Brush(_settings.CalendarColor, 0.4);
        _calMonthLabel.Foreground = brush;
        _calPrev.Foreground = dim;
        _calNext.Foreground = dim;
        foreach (var child in _calHeaders.Children)
        {
            if (child is TextBlock header)
            {
                header.Foreground = dim;
            }
        }

        _calDays.Children.Clear();
        var firstDay = new DateTime(_calYear, _calMonth, 1);
        int startDay = ((int)firstDay.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(_calYear, _calMonth);
        var today = DateTime.Today;
        int row = 0, col = startDay;

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_calYear, _calMonth, d);
            bool isToday = date == today;
            bool hasReminder = _reminderService.HasRemindersOnDate(_reminders, date);
            var cell = new Grid { Cursor = new Cursor(StandardCursorType.Hand), Tag = date };

            if (isToday)
            {
                cell.Children.Add(new Border
                {
                    Background = todayBrush,
                    CornerRadius = new CornerRadius(9),
                    Width = 18,
                    Height = 18,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            cell.Children.Add(new TextBlock
            {
                Text = d.ToString(),
                FontSize = 9,
                Foreground = isToday ? brush : dim,
                FontWeight = isToday ? FontWeight.Bold : FontWeight.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (hasReminder)
            {
                cell.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB)),
                    Width = 4,
                    Height = 4,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1)
                });
            }

            cell.PointerPressed += CalDay_Click;
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            _calDays.Children.Add(cell);

            col++;
            if (col >= 7)
            {
                col = 0;
                row++;
            }
        }

        RefreshReminderDetails();
    }

    private void RefreshReminderDetails()
    {
        var dim = UiTheme.Brush(_settings.CalendarColor, 0.55);
        var monthEnd = new DateTime(_calYear, _calMonth, 1).AddMonths(1).AddDays(-1);
        var today = DateTime.Today;

        var upcoming = _reminders
            .Where(r => r.DateTime.Date >= today && r.DateTime.Date <= monthEnd)
            .OrderBy(r => r.DateTime)
            .Take(4)
            .ToList();

        if (upcoming.Count == 0)
        {
            _reminderDetails.Text = "";
            return;
        }

        _reminderDetails.Text = string.Join("\n", upcoming.Select(r =>
        {
            var day = r.DateTime.Date == today ? "Bugün"
                : r.DateTime.Date == today.AddDays(1) ? "Yarın"
                : r.DateTime.ToString("dd.MM");
            return $"• {day} {r.DateTime:HH:mm} — {r.Title}";
        }));
        _reminderDetails.Foreground = dim;
    }

    private void ShiftMonth(int delta)
    {
        _calMonth += delta;
        if (_calMonth < 1)
        {
            _calMonth = 12;
            _calYear--;
        }
        else if (_calMonth > 12)
        {
            _calMonth = 1;
            _calYear++;
        }
        RefreshCalendar();
    }

    private async void CalDay_Click(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (sender is Control control && control.Tag is DateTime date)
        {
            var now = DateTime.Now;
            var reminder = new Reminder
            {
                DateTime = new DateTime(date.Year, date.Month, date.Day, (now.Hour + 1) % 24, 0, 0),
                Repeat = RepeatType.None,
                ReminderMinutesBefore = 5,
                IsEnabled = true
            };
            var window = new ReminderWindow(reminder);
            await window.ShowDialog(this);
            if (window.Accepted)
            {
                _reminders = _reminderService.Load();
                RefreshCalendar();
            }
        }
    }

    private void CheckReminders()
    {
        var due = _reminderService.GetDueReminders(_reminders, DateTime.Now);
        foreach (var reminder in due)
        {
            ShowReminderNotification(reminder);
            reminder.AdvanceToNext();
        }
        if (due.Count > 0)
        {
            _reminderService.Save(_reminders);
        }
    }

    private void ShowReminderNotification(Reminder reminder)
    {
        try
        {
            var path = reminder.SoundFile;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                path = SoundPlayer.DefaultChimePath;
            }
            if (!string.IsNullOrWhiteSpace(path))
            {
                _reminderPlayer?.Dispose();
                _reminderPlayer = new SoundPlayer(path);
                _reminderPlayer.Load();
                _reminderPlayer.Play();
            }
        }
        catch (Exception ex)
        {
            ReportSoundError(ex);
        }

        ShowNotificationOnWidget($"⏰ {reminder.Title}");
        if (_trayIcon is not null)
        {
            _trayIcon.ToolTipText = $"Hatırlatma — {reminder.DateTime:HH:mm} • {reminder.Title}";
        }
    }

    private void ReportSoundError(Exception ex)
    {
        App.Log(ex);
        if (_soundErrorShown) return;
        _soundErrorShown = true;
        MessageBox.Show(
            $"Ses çalınamadı. Ayrıntılar hata günlüğüne kaydedildi.\n\n{ex.Message}\n\n{App.LogPath}",
            "WindowsClockWidget - Ses", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowNotificationOnWidget(string text)
    {
        _notificationTimer?.Stop();
        _dateText.Text = text;
        _dateText.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0x98, 0xDB));
        _dateText.FontSize = _settings.DateFontSize;
        _dateText.IsVisible = true;
        _dateText.Opacity = 1;

        _notificationTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _notificationTimer.Tick += (_, _) =>
        {
            _notificationTimer.Stop();
            _dateText.Foreground = UiTheme.Brush(_settings.DateColor);
            _dateText.FontSize = _settings.DateFontSize;
            _dateText.FontFamily = new FontFamily(_settings.DateFontFamily);
            _dateText.FontWeight = UiTheme.Weight(_settings.DateFontWeight);
            UpdateClock();
        };
        _notificationTimer.Start();
    }
}
