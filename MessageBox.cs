using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace WindowsClockWidget;

public enum MessageBoxButton
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel
}

public enum MessageBoxImage
{
    None,
    Error,
    Question,
    Warning,
    Information
}

public enum MessageBoxResult
{
    None,
    OK,
    Cancel,
    Yes,
    No
}

public static class MessageBox
{
    internal sealed class ResultHolder
    {
        public MessageBoxResult Value { get; set; }
    }

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None)
    {
        var owner = Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime
            ? lifetime.MainWindow
            : null;
        var window = new MessageWindow(messageBoxText, caption, button, icon);
        var result = MessageBoxResult.None;
        var completed = false;

        async void Run()
        {
            var holder = await window.ShowDialog<ResultHolder>(owner!);
            result = holder.Value;
            completed = true;
        }

        Dispatcher.UIThread.Post(Run);
        while (!completed)
        {
            Dispatcher.UIThread.RunJobs();
            if (!completed) Thread.Sleep(10);
        }
        return result;
    }
}

internal sealed class MessageWindow : Window
{
    public MessageWindow(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Title = title;
        Width = 440;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        CanResize = false;
        Icon = App.LoadWindowIcon();

        var root = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        var glyph = new TextBlock
        {
            Text = icon switch
            {
                MessageBoxImage.Warning => "!",
                MessageBoxImage.Error => "×",
                MessageBoxImage.Question => "?",
                MessageBoxImage.Information => "i",
                _ => ""
            },
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = UiTheme.Brush(icon == MessageBoxImage.Warning ? "#E67E22" : "#3498DB"),
            IsVisible = icon != MessageBoxImage.None
        };
        root.Children.Add(glyph);
        root.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13,
            Foreground = UiTheme.Brush("#1F1E1D")
        });

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        AddButton(row, MessageBoxResult.OK, "Tamam", true);
        if (buttons is MessageBoxButton.OKCancel or MessageBoxButton.YesNoCancel)
        {
            AddButton(row, MessageBoxResult.Cancel, "İptal", false);
        }
        if (buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel)
        {
            AddButton(row, MessageBoxResult.Yes, "Evet", buttons == MessageBoxButton.YesNo);
            AddButton(row, MessageBoxResult.No, "Hayır", buttons != MessageBoxButton.YesNo);
        }
        root.Children.Add(row);
        Content = root;
    }

    private void AddButton(Panel parent, MessageBoxResult result, string text, bool isDefault)
    {
        var button = new Button
        {
            Content = text,
            Width = 90,
            Height = 32,
            IsDefault = isDefault,
            FontSize = 12
        };
        button.Click += (_, _) => Close(new MessageBox.ResultHolder { Value = result });
        parent.Children.Add(button);
    }
}
