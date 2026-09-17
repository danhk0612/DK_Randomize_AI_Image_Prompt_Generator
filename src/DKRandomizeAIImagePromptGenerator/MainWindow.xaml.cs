using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DKRandomizeAIImagePromptGenerator;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        RootNavigationView.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler(RootNavigationView_PointerWheelChanged),
            handledEventsToo: true);

        RootNavigationView.SelectedItem = MixerNavigationItem;
        NavigateTo("Mixer");
    }

    public void NavigateToMixer()
    {
        RootNavigationView.SelectedItem = MixerNavigationItem;
        NavigateTo("Mixer");
    }

    public void ApplyTheme(AppTheme theme)
    {
        RootNavigationView.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private void RootNavigationView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateTo(tag);
        }
    }

    private void RootNavigationView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(RootNavigationView);
        var delta = point.Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var scrollViewer = FindScrollViewerUnderPointer(
            ContentFrame,
            point.Position,
            delta);

        if (scrollViewer is null)
        {
            return;
        }

        var wheelSteps = delta / 120.0;
        var targetOffset = Math.Clamp(
            scrollViewer.VerticalOffset - (wheelSteps * 96.0),
            0,
            scrollViewer.ScrollableHeight);

        if (Math.Abs(targetOffset - scrollViewer.VerticalOffset) < 0.5)
        {
            return;
        }

        scrollViewer.ChangeView(
            horizontalOffset: null,
            verticalOffset: targetOffset,
            zoomFactor: null,
            disableAnimation: true);

        e.Handled = true;
    }

    private ScrollViewer? FindScrollViewerUnderPointer(
        DependencyObject root,
        Point pointerPosition,
        int wheelDelta)
    {
        var candidates = new List<(ScrollViewer Viewer, double Area, int Depth)>();
        CollectScrollableViewers(root, pointerPosition, wheelDelta, 0, candidates);

        return candidates
            .OrderByDescending(candidate => candidate.Depth)
            .ThenBy(candidate => candidate.Area)
            .Select(candidate => candidate.Viewer)
            .FirstOrDefault();
    }

    private void CollectScrollableViewers(
        DependencyObject current,
        Point pointerPosition,
        int wheelDelta,
        int depth,
        List<(ScrollViewer Viewer, double Area, int Depth)> candidates)
    {
        if (current is ScrollViewer scrollViewer &&
            scrollViewer.VerticalScrollMode != ScrollMode.Disabled &&
            scrollViewer.ScrollableHeight > 0 &&
            IsPointerInside(scrollViewer, pointerPosition))
        {
            var canScroll = wheelDelta > 0
                ? scrollViewer.VerticalOffset > 0
                : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;

            if (canScroll)
            {
                candidates.Add((
                    scrollViewer,
                    Math.Max(1, scrollViewer.ActualWidth * scrollViewer.ActualHeight),
                    depth));
            }
        }

        var childCount = VisualTreeHelper.GetChildrenCount(current);
        for (var index = 0; index < childCount; index++)
        {
            CollectScrollableViewers(
                VisualTreeHelper.GetChild(current, index),
                pointerPosition,
                wheelDelta,
                depth + 1,
                candidates);
        }
    }

    private bool IsPointerInside(FrameworkElement element, Point pointerPosition)
    {
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        try
        {
            var topLeft = element.TransformToVisual(RootNavigationView)
                .TransformPoint(new Point(0, 0));

            return pointerPosition.X >= topLeft.X &&
                   pointerPosition.X <= topLeft.X + element.ActualWidth &&
                   pointerPosition.Y >= topLeft.Y &&
                   pointerPosition.Y <= topLeft.Y + element.ActualHeight;
        }
        catch
        {
            return false;
        }
    }

    private void NavigateTo(string tag)
    {
        var pageType = tag switch
        {
            "Mixer" => typeof(MixerPage),
            "Library" => typeof(PromptLibraryPage),
            "History" => typeof(HistoryPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(MixerPage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
