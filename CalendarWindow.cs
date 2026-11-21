using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace WindowsClockWidget;

public class CalendarWindow : Window
{
    public bool Accepted { get; private set; }

    private readonly ReminderService _service = new();
    private List<Reminder> _reminders;
    private int _year, _month;
    private DateTime _selectedDate;
    private readonly TextBlock _monthLabel;
    private readonly Grid _dayHeaders;
    private readonly Grid _dayGrid;
    private readonly ListBox _list;
    private readonly TextBlock _selectedLabel;

    public CalendarWindow(DateTime? selectDate = null)
    {
        _selectedDate = selectDate ?? DateTime.Today;
        _year = _selectedDate.Year;
        _month = _selectedDate.Month;
        _reminders = _service.Load();

        _monthLabel = new TextBlock { FontSize = 14, FontWeight = FontWeight.Bold, HorizontalAlignment = HorizontalAlignment.Center };
        _dayHeaders = new Grid { Margin = new Thickness(0, 0, 0, 4) };
        _dayGrid = new Grid();
        _selectedLabel = new TextBlock { FontSize = 12, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 0, 0, 8) };
        _list = new ListBox { MinHeight = 160, FontSize = 11 };

        var prev = new TextBlock { Text = "◀", FontSize = 14, Opacity = 0.5, Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
        var next = new TextBlock { Text = "▶", FontSize = 14, Opacity = 0.5, Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
        prev.PointerPressed += (_, e) => { ShiftMonth(-1); e.Handled = true; };
        next.PointerPressed += (_, e) => { ShiftMonth(1); e.Handled = true; };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            Margin = new Thickness(0, 0, 0, 10),
            Children = { prev, _monthLabel, next }
        };
        Grid.SetColumn(_monthLabel, 1);
        Grid.SetColumn(next, 2);

        var add = new Button
        {
            Content = "Hatırlatma Ekle",
            Background = UiTheme.Brush("#3498DB"),
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };
        add.Click += AddReminder_Click;

        var left = Group(new StackPanel { Children = { headerGrid, _dayHeaders, _dayGrid } });
        var right = Group(new StackPanel { Children = { _selectedLabel, _list, add } });

        Title = "Takvim";
        Width = 560;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Icon = App.LoadWindowIcon();
        Content = new Border
        {
            Background = UiTheme.Brush("#F7F7F8"),
            Padding = new Thickness(20),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,16,200"),
                Children = { left, right }
            }
        };
        Grid.SetColumn(right, 2);

        BuildHeaders();
        RefreshCalendar();
        UpdateReminderList();
    }

    private static Border Group(Control child) => new()
    {
        Background = UiTheme.Brush("#FFFFFF"),
        CornerRadius = new CornerRadius(8),
        Padding = new Thickness(14),
        Child = child
    };

    private void BuildHeaders()
    {
        _dayHeaders.Children.Clear();
        _dayHeaders.ColumnDefinitions.Clear();
        string[] days = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        var dim = UiTheme.Brush("#666666");
        for (int c = 0; c < 7; c++)
        {
            _dayHeaders.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            var label = new TextBlock
            {
                Text = days[c],
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = dim,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(label, c);
            _dayHeaders.Children.Add(label);
        }
    }

    private void RefreshCalendar()
    {
        _monthLabel.Text = new CultureInfo("tr-TR").DateTimeFormat.MonthNames[_month - 1].ToUpper() + " " + _year;

        var main = UiTheme.Brush("#1F1E1D");
        var dim = UiTheme.Brush("#999999");
        var selectedBrush = UiTheme.Brush("#3498DB");
        var today = DateTime.Today;

        _dayGrid.Children.Clear();
        _dayGrid.RowDefinitions.Clear();
        _dayGrid.ColumnDefinitions.Clear();
        for (int r = 0; r < 6; r++)
        {
            _dayGrid.RowDefinitions.Add(new RowDefinition(32, GridUnitType.Pixel));
        }
        for (int c = 0; c < 7; c++)
        {
            _dayGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
        }

        var firstDay = new DateTime(_year, _month, 1);
        int startDay = ((int)firstDay.DayOfWeek + 6) % 7;
        int daysInMonth = DateTime.DaysInMonth(_year, _month);
        int row = 0, col = startDay;

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(_year, _month, d);
            bool isToday = date == today;
            bool isSelected = date == _selectedDate;
            bool hasReminder = _service.HasRemindersOnDate(_reminders, date);
            var cell = new Grid { Cursor = new Cursor(StandardCursorType.Hand), Tag = date };

            if (isSelected)
            {
                cell.Children.Add(new Border
                {
                    Background = selectedBrush,
                    CornerRadius = new CornerRadius(14),
                    Width = 28,
                    Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }
            else if (isToday)
            {
                cell.Children.Add(new Border
                {
                    Background = UiTheme.Brush("#1F1E1D", 0.08),
                    CornerRadius = new CornerRadius(14),
                    Width = 28,
                    Height = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            cell.Children.Add(new TextBlock
            {
                Text = d.ToString(),
                FontSize = 12,
                Foreground = isSelected ? Brushes.White : isToday ? main : dim,
                FontWeight = isToday || isSelected ? FontWeight.Bold : FontWeight.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (hasReminder)
            {
                cell.Children.Add(new Border
                {
                    Background = selectedBrush,
                    Width = 4,
                    Height = 4,
                    CornerRadius = new CornerRadius(2),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, 0, 1)
                });
            }

            cell.PointerPressed += Day_Click;
            Grid.SetRow(cell, row);
            Grid.SetColumn(cell, col);
            _dayGrid.Children.Add(cell);

            col++;
            if (col >= 7)
            {
                col = 0;
                row++;
            }
        }
    }

    private void UpdateReminderList()
    {
        _selectedLabel.Text = _selectedDate.ToString("dd MMMM yyyy, dddd", new CultureInfo("tr-TR"));
        var items = new StackPanel { Spacing = 6 };
        foreach (var reminder in _service.GetRemindersForDate(_reminders, _selectedDate))
        {
            items.Children.Add(ReminderRow(reminder));
        }
        _list.ItemsSource = new List<Control> { items };
    }

    private Control ReminderRow(Reminder reminder)
    {
        var info = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = reminder.Title, FontWeight = FontWeight.SemiBold, FontSize = 12 },
                new TextBlock { Text = $"{reminder.DateTime:HH:mm} — {new RepeatTypeConverter().Convert(reminder.Repeat, typeof(string), null, CultureInfo.CurrentCulture)}", FontSize = 10, Opacity = 0.6 }
            }
        };
        var edit = new TextBlock { Text = "✏", FontSize = 12, Opacity = 0.5, Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
        edit.PointerPressed += (_, e) => { EditReminder(reminder); e.Handled = true; };
        var delete = new TextBlock { Text = "🗑", FontSize = 12, Opacity = 0.5, Cursor = new Cursor(StandardCursorType.Hand), VerticalAlignment = VerticalAlignment.Center };
        delete.PointerPressed += (_, e) => { DeleteReminder(reminder); e.Handled = true; };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), Margin = new Thickness(0, 0, 0, 6) };
        grid.Children.Add(info);
        grid.Children.Add(edit);
        grid.Children.Add(delete);
        Grid.SetColumn(edit, 1);
        Grid.SetColumn(delete, 2);
        return grid;
    }

    private void Day_Click(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (sender is Control control && control.Tag is DateTime date)
        {
            _selectedDate = date;
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private void ShiftMonth(int delta)
    {
        int month = _month + delta;
        int year = _year;
        if (month < 1) { month = 12; year--; }
        if (month > 12) { month = 1; year++; }
        _month = month;
        _year = year;
        RefreshCalendar();
    }

    private async void AddReminder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var now = DateTime.Now;
        var reminder = new Reminder
        {
            DateTime = new DateTime(_selectedDate.Year, _selectedDate.Month, _selectedDate.Day, (now.Hour + 1) % 24, 0, 0),
            Repeat = RepeatType.None,
            ReminderMinutesBefore = 5,
            IsEnabled = true
        };
        var window = new ReminderWindow(reminder);
        await window.ShowDialog(this);
        if (window.Accepted)
        {
            _reminders = _service.Load();
            _selectedDate = reminder.DateTime.Date;
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private async void EditReminder(Reminder reminder)
    {
        var window = new ReminderWindow(reminder);
        await window.ShowDialog(this);
        if (window.Accepted)
        {
            _reminders = _service.Load();
            RefreshCalendar();
            UpdateReminderList();
        }
    }

    private void DeleteReminder(Reminder reminder)
    {
        var answer = MessageBox.Show($"\"{reminder.Title}\" hatırlatmasını silmek istediğinize emin misiniz?",
            "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;
        _reminders.Remove(reminder);
        _service.Save(_reminders);
        RefreshCalendar();
        UpdateReminderList();
    }
}
