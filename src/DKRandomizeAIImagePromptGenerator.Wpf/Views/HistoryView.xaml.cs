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
        WheelScrollService.Enable(DetailScrollViewer);
        Loaded += HistoryView_Loaded;
    }

    public HistoryViewModel ViewModel { get; }

    private async void HistoryView_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshAsync();
        UpdateEmptyState();
    }

    private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedHistory = HistoryListBox.SelectedItem as CombinationHistory;
        UpdateDetail();
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null) return;
        var app = (App)Application.Current;
        app.PendingHistoryRestore = _selectedHistory;
        app.MainWindowInstance?.NavigateToMixer();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedHistory is null) return;
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
        HistoryListBox.SelectedItem = null;
        UpdateEmptyState();
        UpdateDetail();
    }

    private void UpdateEmptyState() =>
        EmptyState.Visibility = ViewModel.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateDetail()
    {
        if (_selectedHistory is null)
        {
            DetailPane.Visibility = Visibility.Collapsed;
            return;
        }

        DetailPane.Visibility = Visibility.Visible;
        DetailScrollViewer.ScrollToTop();
        CreatedAtText.Text = _selectedHistory.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        CharacterTitleText.Text = _selectedHistory.CharacterTitleSnapshot ?? "없음";
        ArtistTitleText.Text = _selectedHistory.ArtistTitleSnapshot ?? "없음";
        AdditionalTitleText.Text = _selectedHistory.AdditionalItems.Count == 0
            ? "없음"
            : string.Join(", ", _selectedHistory.AdditionalItems.Select(item => item.TitleSnapshot ?? "삭제된 프롬프트"));
        PositiveTextBox.Text = _selectedHistory.PositiveText;
        NegativeTextBox.Text = _selectedHistory.NegativeText;
    }
}
