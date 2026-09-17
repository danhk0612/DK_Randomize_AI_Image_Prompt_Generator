using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var delta = e.GetCurrentPoint(RootNavigationView).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        var scrollViewer = FindScrollableAncestor(source, delta);
        if (scrollViewer is null)
        {
            return;
        }

        var targetOffset = Math.Clamp(
            scrollViewer.VerticalOffset - delta,
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

    private static ScrollViewer? FindScrollableAncestor(DependencyObject source, int wheelDelta)
    {
        DependencyObject? current = source;

        while (current is not null)
        {
            if (current is ScrollViewer scrollViewer &&
                scrollViewer.VerticalScrollMode != ScrollMode.Disabled &&
                scrollViewer.ScrollableHeight > 0)
            {
                var canScroll = wheelDelta > 0
                    ? scrollViewer.VerticalOffset > 0
                    : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;

                if (canScroll)
                {
                    return scrollViewer;
                }
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
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
