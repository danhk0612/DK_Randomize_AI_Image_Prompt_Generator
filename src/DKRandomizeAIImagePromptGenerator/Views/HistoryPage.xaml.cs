using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DKRandomizeAIImagePromptGenerator.Views;

public sealed partial class HistoryPage : Page
{
    private CombinationHistory? _selectedHistory;

    public HistoryPage()
    {
        ViewModel = new HistoryViewModel(((App)Application.Current).History);
        InitializeComponent();
        Loaded += HistoryPage_Loaded;
    }

    public HistoryViewModel ViewModel { get; }

    private async void HistoryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedHistory = HistoryListView.SelectedItem as CombinationHistory;
        UpdateDetail();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null)
        {
            return;
        }

        var app = (App)Application.Current;
        app.PendingHistoryRestore = _selectedHistory;

        if (app.MainWindowInstance is MainWindow window)
        {
            window.NavigateToMixer();
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null)
        {
            return;
        }

        var history = _selectedHistory;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "기록 삭제",
            Content = "선택한 조합 기록을 삭제합니다.",
            PrimaryButtonText = "삭제",
            CloseButtonText = "취소",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeleteAsync(history);
        _selectedHistory = null;
        HistoryListView.SelectedItem = null;
        UpdateEmptyState();
        UpdateDetail();
    }

    private async Task RefreshAsync()
    {
        await ViewModel.RefreshAsync();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = ViewModel.Items.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateDetail()
    {
        if (_selectedHistory is null)
        {
            DetailPane.Visibility = Visibility.Collapsed;
            return;
        }

        DetailPane.Visibility = Visibility.Visible;
        CreatedAtText.Text = _selectedHistory.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        CharacterTitleText.Text = _selectedHistory.CharacterTitleSnapshot ?? "없음";
        ArtistTitleText.Text = _selectedHistory.ArtistTitleSnapshot ?? "없음";
        AdditionalTitleText.Text = _selectedHistory.AdditionalItems.Count == 0
            ? "없음"
            : string.Join(", ", _selectedHistory.AdditionalItems
                .Select(item => item.TitleSnapshot ?? "삭제된 프롬프트"));
        PositiveTextBox.Text = _selectedHistory.PositiveText;
        NegativeTextBox.Text = _selectedHistory.NegativeText;
    }
}
