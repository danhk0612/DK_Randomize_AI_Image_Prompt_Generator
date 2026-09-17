using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DKRandomizeAIImagePromptGenerator;

public sealed partial class MainWindow : Window
{
    private readonly NavigationView _rootNavigationView;
    private readonly NavigationViewItem _mixerNavigationItem;
    private readonly Frame _contentFrame;

    public MainWindow()
    {
        InitializeComponent();

        _contentFrame = new Frame();
        _mixerNavigationItem = CreateNavigationItem("조합", "Mixer", Symbol.Shuffle);

        _rootNavigationView = new NavigationView
        {
            IsBackButtonVisible = NavigationViewBackButtonVisible.Collapsed,
            IsSettingsVisible = false,
            PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
            PaneTitle = "Prompt Generator",
            Content = _contentFrame
        };

        _rootNavigationView.MenuItems.Add(_mixerNavigationItem);
        _rootNavigationView.MenuItems.Add(CreateNavigationItem("프롬프트", "Library", Symbol.Document));
        _rootNavigationView.MenuItems.Add(CreateNavigationItem("최근 기록", "History", Symbol.Clock));
        _rootNavigationView.FooterMenuItems.Add(CreateNavigationItem("설정", "Settings", Symbol.Setting));
        _rootNavigationView.SelectionChanged += RootNavigationView_SelectionChanged;

        RootGrid.Children.Add(_rootNavigationView);

        _rootNavigationView.SelectedItem = _mixerNavigationItem;
        NavigateTo("Mixer");
    }

    public void NavigateToMixer()
    {
        _rootNavigationView.SelectedItem = _mixerNavigationItem;
        NavigateTo("Mixer");
    }

    public void ApplyTheme(AppTheme theme)
    {
        _rootNavigationView.RequestedTheme = theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }

    private static NavigationViewItem CreateNavigationItem(string content, string tag, Symbol symbol) =>
        new()
        {
            Content = content,
            Tag = tag,
            Icon = new SymbolIcon(symbol)
        };

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

        if (_contentFrame.CurrentSourcePageType != pageType)
        {
            _contentFrame.Navigate(pageType);
        }
    }
}
