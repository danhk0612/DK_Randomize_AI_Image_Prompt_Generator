using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DKRandomizeAIImagePromptGenerator.Wpf.Views;

namespace DKRandomizeAIImagePromptGenerator.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NavigateToMixer();
    }

    public void NavigateToMixer()
    {
        PageHost.Content = new MixerView();
        SelectNav(MixerNavButton);
    }

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
