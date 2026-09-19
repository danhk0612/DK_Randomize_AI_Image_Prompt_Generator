using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Services;

public static class ScrollSmokeRunner
{
    public static bool Run()
    {
        var textBox = new TextBox
        {
            Height = 120,
            Text = "Wheel target",
            AcceptsReturn = true
        };

        var content = new StackPanel();
        content.Children.Add(textBox);
        content.Children.Add(new Border { Height = 1800 });

        var scrollViewer = new ScrollViewer
        {
            Height = 320,
            Width = 640,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        WheelScrollService.Enable(scrollViewer);

        var window = new Window
        {
            Width = 680,
            Height = 380,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            Opacity = 0,
            Left = -10000,
            Top = -10000,
            Content = scrollViewer
        };

        try
        {
            window.Show();
            window.UpdateLayout();
            scrollViewer.UpdateLayout();

            if (scrollViewer.ScrollableHeight <= 0)
            {
                return false;
            }

            scrollViewer.ScrollToVerticalOffset(Math.Min(400, scrollViewer.ScrollableHeight));
            scrollViewer.UpdateLayout();
            var before = scrollViewer.VerticalOffset;

            var args = new MouseWheelEventArgs(Mouse.PrimaryDevice, Environment.TickCount, 120)
            {
                RoutedEvent = UIElement.PreviewMouseWheelEvent,
                Source = textBox
            };
            textBox.RaiseEvent(args);
            scrollViewer.UpdateLayout();

            var after = scrollViewer.VerticalOffset;
            return args.Handled && after < before - 1;
        }
        finally
        {
            window.Close();
        }
    }
}
