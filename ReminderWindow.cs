using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.IO;

namespace WindowsClockWidget;

public class ReminderWindow : Window
{
    public bool Accepted { get; private set; }

    private readonly ReminderService _service = new();
    private readonly Reminder _reminder;
    private readonly TextBox _titleBox;
    private readonly DatePicker _datePicker;
    private readonly ComboBox _hourBox;
    private readonly ComboBox _minuteBox;
    private readonly ComboBox _repeatBox;
    private readonly ComboBox _beforeBox;
    private readonly ComboBox _soundBox;

    public ReminderWindow(Reminder reminder)
    {
        _reminder = reminder;
        _titleBox = new TextBox { Width = 300, Height = 30, FontSize = 13 };
        _datePicker = new DatePicker { Width = 300, SelectedDate = reminder.DateTime.Date };
        _hourBox = new ComboBox { Width = 140 };
        _minuteBox = new ComboBox { Width = 140 };
        _repeatBox = new ComboBox { Width = 300 };
        _beforeBox = new ComboBox { Width = 300 };
        _soundBox = new ComboBox { Width = 240 };

        for (int h = 0; h < 24; h++) _hourBox.Items.Add(h.ToString("D2"));
        for (int m = 0; m < 60; m += 5) _minuteBox.Items.Add(m.ToString("D2"));
        AddTagged(_repeatBox, "Yok", "None");
        AddTagged(_repeatBox, "Her gün", "Daily");
        AddTagged(_repeatBox, "Her hafta", "Weekly");
        AddTagged(_repeatBox, "Her ay", "Monthly");
        AddTagged(_beforeBox, "Hemen", "0");
        AddTagged(_beforeBox, "5 dakika önce", "5");
        AddTagged(_beforeBox, "10 dakika önce", "10");
        AddTagged(_beforeBox, "15 dakika önce", "15");
        AddTagged(_beforeBox, "30 dakika önce", "30");
        AddTagged(_beforeBox, "1 saat önce", "60");
        LoadSounds();

        _titleBox.Text = reminder.Title == "Hatırlatma" ? "" : reminder.Title;
        _hourBox.SelectedIndex = reminder.DateTime.Hour;
        _minuteBox.SelectedIndex = reminder.DateTime.Minute / 5;
        SelectTagged(_repeatBox, reminder.Repeat.ToString(), "None");
        SelectTagged(_beforeBox, reminder.ReminderMinutesBefore.ToString(), "5");
        if (!string.IsNullOrEmpty(reminder.SoundFile))
        {
            var match = _soundBox.Items.OfType<ComboBoxItem>()
                .FirstOrDefault(i => string.Equals(i.Tag as string, reminder.SoundFile, StringComparison.OrdinalIgnoreCase));
            if (match is not null) _soundBox.SelectedItem = match;
        }
        _soundBox.SelectedItem ??= _soundBox.Items.OfType<ComboBoxItem>().FirstOrDefault();

        var listen = new Button
        {
            Content = "Dinle",
            Width = 60,
            Background = UiTheme.Brush("#E4E4E6"),
            Foreground = UiTheme.Brush("#1F1E1D")
        };
        listen.Click += PreviewSound_Click;

        var save = new Button
        {
            Content = "Kaydet",
            Width = 100,
            Background = UiTheme.Brush("#1F1E1D"),
            Foreground = Brushes.White
        };
        save.Click += Save_Click;
        var cancel = new Button
        {
            Content = "Vazgeç",
            Width = 100,
            Background = UiTheme.Brush("#E4E4E6"),
            Foreground = UiTheme.Brush("#1F1E1D")
        };
        cancel.Click += (_, _) => Close();

        var root = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 4,
            Children =
            {
                Field("Başlık"), _titleBox,
                Field("Tarih"), _datePicker,
                Field("Saat"),
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto,*"),
                    Children = { _hourBox, new TextBlock { Text = ":", FontSize = 16, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }, _minuteBox }
                },
                Field("Tekrar"), _repeatBox,
                Field("Hatırlatma zamanı"), _beforeBox,
                Field("Ses"),
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { _soundBox, listen } },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Margin = new Thickness(0, 12, 0, 0),
                    Children = { save, cancel }
                }
            }
        };
        var colon = root.Children[5] as Grid;
        if (colon is not null) Grid.SetColumn(colon.Children[1], 1);
        if (colon is not null) Grid.SetColumn(colon.Children[2], 2);

        Title = "Hatırlatma";
        Width = 380;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Icon = App.LoadWindowIcon();
        Content = new Border { Background = UiTheme.Brush("#F7F7F8"), Child = root };
    }

    private static TextBlock Field(string text) => new()
    {
        Text = text,
        FontSize = 11,
        Foreground = UiTheme.Brush("#666666"),
        Margin = new Thickness(0, 4, 0, 0)
    };

    private static void AddTagged(ComboBox box, string content, string tag)
        => box.Items.Add(new ComboBoxItem { Content = content, Tag = tag });

    private static void SelectTagged(ComboBox box, string tag, string fallback)
    {
        var item = box.Items.OfType<ComboBoxItem>().FirstOrDefault(i => Equals(i.Tag, tag))
                   ?? box.Items.OfType<ComboBoxItem>().First(i => Equals(i.Tag, fallback));
        box.SelectedItem = item;
    }

    private void LoadSounds()
    {
        try
        {
            foreach (var file in SoundPlayer.SoundFiles())
            {
                _soundBox.Items.Add(new ComboBoxItem { Content = Path.GetFileName(file), Tag = file });
            }
        }
        catch (Exception ex)
        {
            App.Log(ex);
        }
    }

    private async void PreviewSound_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var path = (_soundBox.SelectedItem as ComboBoxItem)?.Tag as string;
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

    private void Save_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _reminder.Title = string.IsNullOrWhiteSpace(_titleBox.Text) ? "Hatırlatma" : _titleBox.Text.Trim();
        var date = _datePicker.SelectedDate ?? DateTime.Now;
        int hour = _hourBox.SelectedIndex >= 0 ? _hourBox.SelectedIndex : 0;
        int minute = _minuteBox.SelectedIndex >= 0 ? _minuteBox.SelectedIndex * 5 : 0;
        _reminder.DateTime = new DateTime(date.Year, date.Month, date.Day, hour, minute, 0);
        _reminder.Repeat = Enum.TryParse<RepeatType>((_repeatBox.SelectedItem as ComboBoxItem)?.Tag as string, out var repeat)
            ? repeat : RepeatType.None;
        _reminder.SoundFile = (_soundBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        _reminder.ReminderMinutesBefore = int.TryParse((_beforeBox.SelectedItem as ComboBoxItem)?.Tag as string, out var before) ? before : 5;
        _reminder.IsEnabled = true;

        var all = _service.Load();
        int index = all.FindIndex(r => r.Id == _reminder.Id);
        if (index >= 0) all[index] = _reminder;
        else all.Add(_reminder);
        _service.Save(all);
        Accepted = true;
        Close();
    }
}
