using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Wpf.Views;

namespace DKRandomizeAIImagePromptGenerator.Wpf;

public partial class MainWindow : Window
{
    private readonly MixerView _mixerView;

    public MainWindow()
    {
        InitializeComponent();
        RestoreWindowPlacement();

        _mixerView = new MixerView();
        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
        NavigateToMixer();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowPlacement();
        base.OnClosing(e);
    }

    private void RestoreWindowPlacement()
    {
        var settings = ((App)Application.Current).Settings.Current;
        if (settings.WindowWidth is not double savedWidth ||
            settings.WindowHeight is not double savedHeight ||
            !double.IsFinite(savedWidth) ||
            !double.IsFinite(savedHeight) ||
            savedWidth <= 0 ||
            savedHeight <= 0)
        {
            return;
        }

        Width = Math.Clamp(
            savedWidth,
            MinWidth,
            Math.Max(MinWidth, SystemParameters.VirtualScreenWidth));
        Height = Math.Clamp(
            savedHeight,
            MinHeight,
            Math.Max(MinHeight, SystemParameters.VirtualScreenHeight));

        if (settings.WindowLeft is double savedLeft &&
            settings.WindowTop is double savedTop &&
            double.IsFinite(savedLeft) &&
            double.IsFinite(savedTop) &&
            IsPlacementVisible(savedLeft, savedTop, Width, Height))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = savedLeft;
            Top = savedTop;
        }

        if (settings.WindowMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveWindowPlacement()
    {
        var settingsService = ((App)Application.Current).Settings;
        var settings = settingsService.Current;
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        if (!bounds.IsEmpty &&
            double.IsFinite(bounds.Left) &&
            double.IsFinite(bounds.Top) &&
            double.IsFinite(bounds.Width) &&
            double.IsFinite(bounds.Height) &&
            bounds.Width > 0 &&
            bounds.Height > 0)
        {
            settings.WindowLeft = bounds.Left;
            settings.WindowTop = bounds.Top;
            settings.WindowWidth = bounds.Width;
            settings.WindowHeight = bounds.Height;
        }

        settings.WindowMaximized = WindowState == WindowState.Maximized;

        try
        {
            settingsService.SaveAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Window placement persistence must not block application shutdown.
        }
    }

    private static bool IsPlacementVisible(
        double left,
        double top,
        double width,
        double height)
    {
        var virtualLeft = SystemParameters.VirtualScreenLeft;
        var virtualTop = SystemParameters.VirtualScreenTop;
        var virtualRight = virtualLeft + SystemParameters.VirtualScreenWidth;
        var virtualBottom = virtualTop + SystemParameters.VirtualScreenHeight;

        var visibleWidth = Math.Min(left + width, virtualRight) - Math.Max(left, virtualLeft);
        var visibleHeight = Math.Min(top + height, virtualBottom) - Math.Max(top, virtualTop);

        return visibleWidth >= 100 && visibleHeight >= 60;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) =>
        ApplyResponsiveShell(ActualWidth);

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyResponsiveShell(e.NewSize.Width);

    private void ApplyResponsiveShell(double availableWidth)
    {
        var compact = availableWidth < 1000;
        NavigationColumn.Width = new GridLength(compact ? 64 : 220);
        BrandPanel.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;

        ConfigureNavButton(MixerNavButton, compact ? "⤨" : "⤨  프롬프트 조합", compact);
        ConfigureNavButton(LibraryNavButton, compact ? "▦" : "▦  프롬프트 관리", compact);
        ConfigureNavButton(HistoryNavButton, compact ? "◷" : "◷  최근 기록", compact);
        ConfigureNavButton(SettingsNavButton, compact ? "⚙" : "⚙  설정", compact);
    }

    private static void ConfigureNavButton(Button button, string content, bool compact)
    {
        button.Content = content;
        button.HorizontalContentAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        button.Padding = compact ? new Thickness(8, 10, 8, 10) : new Thickness(14, 10, 14, 10);
    }

    public void NavigateToMixer()
    {
        PageHost.Content = _mixerView;
        SelectNav(MixerNavButton);
    }

    public bool AddPromptToMixer(PromptItem item) =>
        _mixerView.AddPromptFromLibrary(item);

    public void NotifyPromptChanged(PromptItem item) =>
        _mixerView.NotifyPromptChanged(item);

    public void NotifyPromptDeleted(PromptItem item) =>
        _mixerView.NotifyPromptDeleted(item);

    private void MixerNavButton_Click(object sender, RoutedEventArgs e) => NavigateToMixer();

    private void LibraryNavButton_Click(object sender, RoutedEventArgs e)
    {
        PageHost.Content = new PromptLibraryView();
        SelectNav(LibraryNavButton);
    }

    private void HistoryNavButton_Click(object sender, RoutedEventArgs e)
    {
        PageHost.Content = new HistoryView();
        SelectNav(HistoryNavButton);
    }

    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
    {
        PageHost.Content = new SettingsView();
        SelectNav(SettingsNavButton);
    }

    private void SelectNav(Button selected)
    {
        var buttons = new[] { MixerNavButton, LibraryNavButton, HistoryNavButton, SettingsNavButton };
        foreach (var button in buttons)
        {
            button.Background = Brushes.Transparent;
            button.SetResourceReference(Control.ForegroundProperty, "TextPrimaryBrush");
            button.FontWeight = FontWeights.Normal;
        }

        selected.SetResourceReference(Control.BackgroundProperty, "SelectionBackgroundBrush");
        selected.SetResourceReference(Control.ForegroundProperty, "AccentBrush");
        selected.FontWeight = FontWeights.SemiBold;
    }
}
