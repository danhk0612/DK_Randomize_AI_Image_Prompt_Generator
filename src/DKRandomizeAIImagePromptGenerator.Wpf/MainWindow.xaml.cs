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
        _mixerView = new MixerView();
        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
        NavigateToMixer();
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
            button.FontWeight = FontWeights.Normal;
        }

        selected.Background = (Brush)FindResource("SurfaceBrush");
        selected.FontWeight = FontWeights.SemiBold;
    }
}
