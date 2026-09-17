using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using DKRandomizeAIImagePromptGenerator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DKRandomizeAIImagePromptGenerator;

public sealed partial class MainWindow : Window
{
    private readonly ScrollDiagnosticsService _scrollDiagnostics;

    public MainWindow()
    {
        InitializeComponent();

        _scrollDiagnostics = new ScrollDiagnosticsService(
            this,
            RootNavigationView,
            ContentFrame);

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
