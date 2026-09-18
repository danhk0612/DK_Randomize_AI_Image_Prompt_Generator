using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class HistoryView : UserControl
{
    private CombinationHistory? _selectedHistory;

    public HistoryView()
    {
        ViewModel = new HistoryViewModel(((App)Application.Current).History);
        InitializeComponent();
        DataContext = ViewModel;
        PromptPreviewItems.ItemsSource = PreviewItems;
        WheelScrollService.Enable(DetailScrollViewer);
        Loaded += HistoryView_Loaded;
    }

    public HistoryViewModel ViewModel { get; }

    public ObservableCollection<HistoryPromptPreviewItem> PreviewItems { get; } = [];

    private async void HistoryView_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshPageAsync();
    }

    private async void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedHistory = HistoryListBox.SelectedItem as CombinationHistory;
        await UpdateDetailAsync();
    }

    private async void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasPreviousPage)
        {
            return;
        }

        _selectedHistory = null;
        await ViewModel.PreviousPageAsync();
        AfterPageChanged();
    }

    private async void NextPage_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.HasNextPage)
        {
            return;
        }

        _selectedHistory = null;
        await ViewModel.NextPageAsync();
        AfterPageChanged();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null)
        {
            return;
        }

        var app = (App)Application.Current;
        app.PendingHistoryRestore = _selectedHistory;
        app.MainWindowInstance?.NavigateToMixer();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null)
        {
            return;
        }

        if (MessageBox.Show(
                "선택한 조합 기록을 삭제합니다.",
                "기록 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        await ViewModel.DeleteAsync(_selectedHistory);
        _selectedHistory = null;
        AfterPageChanged();
    }

    private async Task RefreshPageAsync()
    {
        await ViewModel.RefreshAsync();
        AfterPageChanged();
    }

    private void AfterPageChanged()
    {
        HistoryListBox.SelectedItem = null;
        UpdateEmptyState();
        UpdatePagingControls();
        ClearDetail();
    }

    private void UpdateEmptyState() =>
        EmptyState.Visibility = ViewModel.TotalCount == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    private void UpdatePagingControls()
    {
        PageText.Text = $"{ViewModel.CurrentPage} / {ViewModel.TotalPages}";
        HistoryCountText.Text = $"총 {ViewModel.TotalCount}개 · 페이지당 {HistoryViewModel.PageSize}개";
        PreviousPageButton.IsEnabled = ViewModel.HasPreviousPage;
        NextPageButton.IsEnabled = ViewModel.HasNextPage;
    }

    private async Task UpdateDetailAsync()
    {
        if (_selectedHistory is null)
        {
            ClearDetail();
            return;
        }

        DetailPane.Visibility = Visibility.Visible;
        DetailScrollViewer.ScrollToTop();

        CreatedAtText.Text = _selectedHistory.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        CharacterTitleText.Text = _selectedHistory.CharacterTitleSummary;
        ArtistTitleText.Text = _selectedHistory.ArtistTitleSummary;
        AdditionalTitleText.Text = _selectedHistory.AdditionalTitleSummary;

        CharacterModeText.Text = BuildModeText(_selectedHistory, PromptCategory.Character);
        ArtistModeText.Text = BuildModeText(_selectedHistory, PromptCategory.Artist);
        AdditionalModeText.Text = BuildModeText(_selectedHistory, PromptCategory.Additional);

        PositiveTextBox.Text = _selectedHistory.PositiveText;
        NegativeTextBox.Text = _selectedHistory.NegativeText;

        await LoadPreviewItemsAsync(_selectedHistory);
    }

    private async Task LoadPreviewItemsAsync(CombinationHistory history)
    {
        PreviewItems.Clear();
        var app = (App)Application.Current;

        foreach (var category in Enum.GetValues<PromptCategory>())
        {
            foreach (var historyItem in history.GetItems(category))
            {
                PromptItem? current = null;
                if (historyItem.PromptId is Guid id)
                {
                    current = await app.Prompts.GetByIdAsync(id);
                }

                PreviewItems.Add(new HistoryPromptPreviewItem(
                    CategoryLabel(category),
                    historyItem.TitleSnapshot ?? current?.Title ?? "삭제된 프롬프트",
                    current?.ImagePath));
            }
        }

        NoPreviewItemsText.Visibility = PreviewItems.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ClearDetail()
    {
        DetailPane.Visibility = Visibility.Collapsed;
        PreviewItems.Clear();
        NoPreviewItemsText.Visibility = Visibility.Collapsed;
        PositiveTextBox.Text = string.Empty;
        NegativeTextBox.Text = string.Empty;
    }

    private static string BuildModeText(
        CombinationHistory history,
        PromptCategory category)
    {
        var mode = history.GetMode(category);
        var count = history.GetItems(category).Count;

        return mode switch
        {
            PromptSelectionMode.Fixed => $"직접 선택 · {count}개",
            PromptSelectionMode.Random => $"랜덤 {history.GetRandomCount(category)}개 · 저장 결과 {count}개",
            _ => "미사용"
        };
    }

    private static string CategoryLabel(PromptCategory category) => category switch
    {
        PromptCategory.Character => "캐릭터",
        PromptCategory.Artist => "작가 / 스타일",
        PromptCategory.Additional => "추가",
        _ => "프롬프트"
    };
}

public sealed record HistoryPromptPreviewItem(
    string CategoryLabel,
    string Title,
    string? ImagePath);
