using System.Runtime.InteropServices;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace DKRandomizeAIImagePromptGenerator;

public sealed partial class MainWindow : Window
{
    private const int GwlWndProc = -4;
    private const uint WmMouseWheel = 0x020A;
    private const uint WmNcDestroy = 0x0082;

    private readonly WndProcDelegate _wndProcDelegate;
    private nint _windowHandle;
    private nint _previousWndProc;
    private bool _wheelHookInstalled;

    public MainWindow()
    {
        _wndProcDelegate = WindowProc;
        InitializeComponent();

        RootNavigationView.Loaded += RootNavigationView_Loaded;
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

    private void RootNavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        InstallMouseWheelHook();
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

    private void InstallMouseWheelHook()
    {
        if (_wheelHookInstalled)
        {
            return;
        }

        _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (_windowHandle == 0)
        {
            return;
        }

        _previousWndProc = SetWindowLongPtr(
            _windowHandle,
            GwlWndProc,
            Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        if (_previousWndProc != 0)
        {
            _wheelHookInstalled = true;
        }
    }

    private nint WindowProc(nint hWnd, uint message, nint wParam, nint lParam)
    {
        if (message == WmMouseWheel && TryHandleMouseWheel(wParam))
        {
            return 0;
        }

        if (message == WmNcDestroy && _wheelHookInstalled)
        {
            var previous = _previousWndProc;
            _wheelHookInstalled = false;
            _previousWndProc = 0;
            SetWindowLongPtr(hWnd, GwlWndProc, previous);
            return CallWindowProc(previous, hWnd, message, wParam, lParam);
        }

        return _previousWndProc != 0
            ? CallWindowProc(_previousWndProc, hWnd, message, wParam, lParam)
            : DefWindowProc(hWnd, message, wParam, lParam);
    }

    private bool TryHandleMouseWheel(nint wParam)
    {
        var delta = unchecked((short)(((long)wParam >> 16) & 0xFFFF));
        if (delta == 0 || _windowHandle == 0)
        {
            return false;
        }

        if (!GetCursorPos(out var screenPoint))
        {
            return false;
        }

        var clientPoint = screenPoint;
        if (!ScreenToClient(_windowHandle, ref clientPoint))
        {
            return false;
        }

        var candidates = new List<(ScrollViewer Viewer, int Depth, double Area)>();
        CollectScrollViewers(
            ContentFrame,
            new Point(clientPoint.X, clientPoint.Y),
            delta,
            0,
            candidates);

        var target = candidates
            .OrderByDescending(candidate => candidate.Depth)
            .ThenBy(candidate => candidate.Area)
            .Select(candidate => candidate.Viewer)
            .FirstOrDefault();

        if (target is null)
        {
            return false;
        }

        var wheelSteps = delta / 120.0;
        var newOffset = Math.Clamp(
            target.VerticalOffset - (wheelSteps * 96.0),
            0,
            target.ScrollableHeight);

        if (Math.Abs(newOffset - target.VerticalOffset) < 0.5)
        {
            return false;
        }

        target.ChangeView(
            horizontalOffset: null,
            verticalOffset: newOffset,
            zoomFactor: null,
            disableAnimation: true);

        return true;
    }

    private void CollectScrollViewers(
        DependencyObject current,
        Point pointerClientPosition,
        int wheelDelta,
        int depth,
        List<(ScrollViewer Viewer, int Depth, double Area)> candidates)
    {
        if (current is ScrollViewer scrollViewer &&
            scrollViewer.Visibility == Visibility.Visible &&
            scrollViewer.VerticalScrollMode != ScrollMode.Disabled &&
            scrollViewer.ScrollableHeight > 0 &&
            IsPointerInside(scrollViewer, pointerClientPosition))
        {
            var canScroll = wheelDelta > 0
                ? scrollViewer.VerticalOffset > 0
                : scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight;

            if (canScroll)
            {
                candidates.Add((
                    scrollViewer,
                    depth,
                    Math.Max(1, scrollViewer.ActualWidth * scrollViewer.ActualHeight)));
            }
        }

        var childCount = VisualTreeHelper.GetChildrenCount(current);
        for (var index = 0; index < childCount; index++)
        {
            CollectScrollViewers(
                VisualTreeHelper.GetChild(current, index),
                pointerClientPosition,
                wheelDelta,
                depth + 1,
                candidates);
        }
    }

    private bool IsPointerInside(FrameworkElement element, Point pointerClientPosition)
    {
        if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        try
        {
            var topLeft = element.TransformToVisual(RootNavigationView)
                .TransformPoint(new Point(0, 0));

            return pointerClientPosition.X >= topLeft.X &&
                   pointerClientPosition.X <= topLeft.X + element.ActualWidth &&
                   pointerClientPosition.Y >= topLeft.Y &&
                   pointerClientPosition.Y <= topLeft.Y + element.ActualHeight;
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

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WndProcDelegate(nint hWnd, uint message, nint wParam, nint lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int index, nint newLong);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(
        nint previousWndProc,
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(
        nint hWnd,
        uint message,
        nint wParam,
        nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint hWnd, ref NativePoint point);
}
