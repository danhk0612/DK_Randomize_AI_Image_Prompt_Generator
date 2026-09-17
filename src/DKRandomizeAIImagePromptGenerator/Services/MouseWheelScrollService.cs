using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DKRandomizeAIImagePromptGenerator.Services;

public static class MouseWheelScrollService
{
    public static void Attach(UIElement element)
    {
        element.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler(OnPointerWheelChanged),
            handledEventsToo: true);
    }

    private static void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not UIElement element)
        {
            return;
        }

        var delta = e.GetCurrentPoint(element).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var scrollViewer = element as ScrollViewer ?? FindScrollableDescendant(element);
        if (scrollViewer is null || scrollViewer.ScrollableHeight <= 0)
        {
            return;
        }

        var step = Math.Abs(delta) >= 120
            ? delta / 120.0 * 72.0
            : Math.Sign(delta) * 72.0;

        var target = Math.Clamp(
            scrollViewer.VerticalOffset - step,
            0,
            scrollViewer.ScrollableHeight);

        if (Math.Abs(target - scrollViewer.VerticalOffset) < 0.5)
        {
            return;
        }

        scrollViewer.ChangeView(null, target, null, disableAnimation: true);
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollableDescendant(DependencyObject root)
    {
        ScrollViewer? fallback = null;
        var count = VisualTreeHelper.GetChildrenCount(root);

        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer scrollViewer)
            {
                fallback ??= scrollViewer;
                if (scrollViewer.ScrollableHeight > 0)
                {
                    return scrollViewer;
                }
            }

            var nested = FindScrollableDescendant(child);
            if (nested is not null)
            {
                if (nested.ScrollableHeight > 0)
                {
                    return nested;
                }

                fallback ??= nested;
            }
        }

        return fallback;
    }
}
