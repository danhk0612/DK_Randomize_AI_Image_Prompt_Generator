using System.Windows.Controls;
using System.Windows.Input;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Services;

public static class WheelScrollService
{
    public static void Enable(ScrollViewer scrollViewer, double multiplier = 0.7)
    {
        scrollViewer.PreviewMouseWheel += (_, e) =>
        {
            if (scrollViewer.ScrollableHeight <= 0)
            {
                return;
            }

            var target = Math.Clamp(
                scrollViewer.VerticalOffset - (e.Delta * multiplier),
                0,
                scrollViewer.ScrollableHeight);

            scrollViewer.ScrollToVerticalOffset(target);
            e.Handled = true;
        };
    }
}
