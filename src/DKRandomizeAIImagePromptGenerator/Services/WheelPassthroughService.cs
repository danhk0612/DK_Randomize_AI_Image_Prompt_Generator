using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DKRandomizeAIImagePromptGenerator.Services;

public static class WheelPassthroughService
{
    public static void Attach(ScrollViewer outerScrollViewer, params UIElement[] interceptors)
    {
        foreach (var interceptor in interceptors)
        {
            interceptor.AddHandler(
                UIElement.PointerWheelChangedEvent,
                new PointerEventHandler((_, e) => ForwardIfNeeded(interceptor, outerScrollViewer, e)),
                handledEventsToo: true);
        }
    }

    private static void ForwardIfNeeded(
        UIElement interceptor,
        ScrollViewer outerScrollViewer,
        PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(interceptor).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var innerScrollViewer = FindScrollableDescendant(interceptor);
        if (innerScrollViewer is not null && CanScrollInDirection(innerScrollViewer, delta))
        {
            return;
        }

        if (!CanScrollInDirection(outerScrollViewer, delta))
        {
            return;
        }

        var wheelSteps = Math.Abs(delta) >= 120
            ? delta / 120.0
            : Math.Sign(delta);

        var targetOffset = Math.Clamp(
            outerScrollViewer.VerticalOffset - (wheelSteps * 72.0),
            0,
            outerScrollViewer.ScrollableHeight);

        if (Math.Abs(targetOffset - outerScrollViewer.VerticalOffset) < 0.5)
        {
            return;
        }

        outerScrollViewer.ChangeView(
            horizontalOffset: null,
            verticalOffset: targetOffset,
            zoomFactor: null,
            disableAnimation: true);

        e.Handled = true;
    }

    private static bool CanScrollInDirection(ScrollViewer scrollViewer, int delta)
    {
        if (scrollViewer.VerticalScrollMode == ScrollMode.Disabled || scrollViewer.ScrollableHeight <= 0)
        {
            return false;
        }

        return delta > 0
            ? scrollViewer.VerticalOffset > 0.5
            : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight - 0.5;
    }

    private static ScrollViewer? FindScrollableDescendant(DependencyObject root)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        ScrollViewer? fallback = null;

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
