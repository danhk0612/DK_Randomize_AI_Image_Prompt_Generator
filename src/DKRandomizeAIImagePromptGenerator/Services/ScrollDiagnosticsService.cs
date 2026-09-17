using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class ScrollDiagnosticsService
{
    private const int GwlWndProc = -4;
    private const uint WmMouseWheel = 0x020A;
    private const uint WmNcDestroy = 0x0082;

    private readonly FrameworkElement _eventRoot;
    private readonly DependencyObject _visualRoot;
    private readonly string _logPath;
    private readonly WndProcDelegate _wndProcDelegate;

    private nint _windowHandle;
    private nint _previousWndProc;
    private bool _installed;

    public ScrollDiagnosticsService(Window window, FrameworkElement eventRoot, DependencyObject visualRoot)
    {
        _eventRoot = eventRoot;
        _visualRoot = visualRoot;
        _wndProcDelegate = WindowProc;

        var rootDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DK Randomize AI Image Prompt Generator");
        Directory.CreateDirectory(rootDirectory);
        _logPath = Path.Combine(rootDirectory, "scroll-diagnostics.log");

        try
        {
            File.WriteAllText(
                _logPath,
                $"=== Scroll diagnostics started {DateTimeOffset.Now:O} ==={Environment.NewLine}" +
                $"OS={Environment.OSVersion}; Runtime={Environment.Version}{Environment.NewLine}");
        }
        catch
        {
        }

        eventRoot.AddHandler(
            UIElement.PointerWheelChangedEvent,
            new PointerEventHandler(OnPointerWheelChanged),
            handledEventsToo: true);

        eventRoot.Loaded += (_, _) => InstallNativeHook(window);
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            var point = e.GetCurrentPoint(_eventRoot);
            var sourceType = e.OriginalSource?.GetType().FullName ?? "<null>";
            WriteLine(
                $"POINTER wheel delta={point.Properties.MouseWheelDelta}; handled={e.Handled}; source={sourceType}; pos=({point.Position.X:F1},{point.Position.Y:F1})");
            DumpScrollViewers("POINTER");
        }
        catch (Exception ex)
        {
            WriteLine($"POINTER diagnostics error: {ex}");
        }
    }

    private void InstallNativeHook(Window window)
    {
        if (_installed)
        {
            return;
        }

        try
        {
            _windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (_windowHandle == 0)
            {
                WriteLine("NATIVE hook skipped: hwnd=0");
                return;
            }

            _previousWndProc = SetWindowLongPtr(
                _windowHandle,
                GwlWndProc,
                Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

            _installed = _previousWndProc != 0;
            WriteLine($"NATIVE hook installed={_installed}; hwnd=0x{_windowHandle:X}; previous=0x{_previousWndProc:X}");
        }
        catch (Exception ex)
        {
            WriteLine($"NATIVE hook install error: {ex}");
        }
    }

    private nint WindowProc(nint hWnd, uint message, nint wParam, nint lParam)
    {
        if (message == WmMouseWheel)
        {
            try
            {
                var delta = unchecked((short)(((long)wParam >> 16) & 0xFFFF));
                WriteLine($"NATIVE WM_MOUSEWHEEL delta={delta}; wParam=0x{wParam:X}; lParam=0x{lParam:X}");
                _eventRoot.DispatcherQueue.TryEnqueue(() => DumpScrollViewers("NATIVE"));
            }
            catch (Exception ex)
            {
                WriteLine($"NATIVE diagnostics error: {ex}");
            }
        }

        if (message == WmNcDestroy && _installed)
        {
            var previous = _previousWndProc;
            _installed = false;
            _previousWndProc = 0;
            SetWindowLongPtr(hWnd, GwlWndProc, previous);
            return CallWindowProc(previous, hWnd, message, wParam, lParam);
        }

        return _previousWndProc != 0
            ? CallWindowProc(_previousWndProc, hWnd, message, wParam, lParam)
            : DefWindowProc(hWnd, message, wParam, lParam);
    }

    private void DumpScrollViewers(string source)
    {
        try
        {
            var viewers = new List<ScrollViewer>();
            CollectScrollViewers(_visualRoot, viewers);
            WriteLine($"{source} scrollViewers={viewers.Count}");

            for (var index = 0; index < viewers.Count; index++)
            {
                var viewer = viewers[index];
                WriteLine(
                    $"  [{index}] name={viewer.Name}; visible={viewer.Visibility}; mode={viewer.VerticalScrollMode}; " +
                    $"offset={viewer.VerticalOffset:F1}; scrollable={viewer.ScrollableHeight:F1}; " +
                    $"viewport={viewer.ViewportHeight:F1}; extent={viewer.ExtentHeight:F1}; " +
                    $"size={viewer.ActualWidth:F1}x{viewer.ActualHeight:F1}");
            }
        }
        catch (Exception ex)
        {
            WriteLine($"{source} scroll viewer dump error: {ex}");
        }
    }

    private static void CollectScrollViewers(DependencyObject current, List<ScrollViewer> viewers)
    {
        if (current is ScrollViewer scrollViewer)
        {
            viewers.Add(scrollViewer);
        }

        var count = VisualTreeHelper.GetChildrenCount(current);
        for (var index = 0; index < count; index++)
        {
            CollectScrollViewers(VisualTreeHelper.GetChild(current, index), viewers);
        }
    }

    private void WriteLine(string message)
    {
        try
        {
            File.AppendAllText(
                _logPath,
                $"[{DateTimeOffset.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint WndProcDelegate(nint hWnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int index, nint newLong);

    [DllImport("user32.dll")]
    private static extern nint CallWindowProc(nint previousWndProc, nint hWnd, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll")]
    private static extern nint DefWindowProc(nint hWnd, uint message, nint wParam, nint lParam);
}
