using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class PromptDuplicateDialog : Window
{
    private readonly PromptCategory _category;
    private readonly App _app;
    private bool _refreshing;

    public PromptDuplicateDialog(PromptCategory category)
    {
        _category = category;
        _app = (App)Application.Current;

        InitializeComponent();
        DataContext = this;
        CategoryText.Text = $"대상 분류: {GetCategoryName(category)}";
        Loaded += PromptDuplicateDialog_Loaded;
    }

    public ObservableCollection<PromptDuplicatePairRow> Rows { get; } = [];

    public int DeletedCount { get; private set; }

    private async void PromptDuplicateDialog_Loaded(
        object sender,
        RoutedEventArgs e) =>
        await RefreshAsync();

    private async void Refresh_Click(object sender, RoutedEventArgs e) =>
        await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        StatusText.Text = "검사 중...";

        try
        {
            var items = await _app.Prompts.SearchAsync(_category);
            var pairs = _app.PromptDuplicates.FindPairs(items);

            Rows.Clear();
            foreach (var pair in pairs)
            {
                Rows.Add(new PromptDuplicatePairRow(pair));
            }

            var exactCount = pairs.Count(pair => pair.IsExact);
            var similarCount = pairs.Count - exactCount;
            SummaryText.Text =
                $"완전 중복 {exactCount}쌍 · 매우 유사 {similarCount}쌍 · 전체 {pairs.Count}쌍";
            StatusText.Text = pairs.Count == 0
                ? "중복 또는 매우 유사한 프롬프트가 없습니다."
                : "삭제할 항목이 있다면 A 삭제 또는 B 삭제를 선택하세요.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"검사 실패: {ex.Message}";
            MessageBox.Show(
                ex.Message,
                "중복 검사 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_refreshing ||
            sender is not Button button ||
            button.Tag is not PromptItem item)
        {
            return;
        }

        if (MessageBox.Show(
                $"'{item.Title}' 프롬프트를 삭제합니다.\n최근 기록의 최종 텍스트는 유지됩니다.",
                "중복 프롬프트 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await _app.Prompts.DeleteAsync(item.Id);
            await _app.Images.DeleteIfUnreferencedAsync(item.ImagePath);
            _app.MainWindowInstance?.NotifyPromptDeleted(item);
            DeletedCount++;
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "프롬프트 삭제 실패",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private static string GetCategoryName(PromptCategory category) => category switch
    {
        PromptCategory.Artist => "작가 / 스타일",
        PromptCategory.Additional => "추가",
        _ => "캐릭터"
    };
}

public sealed class PromptDuplicatePairRow
{
    public PromptDuplicatePairRow(
        PromptDuplicatePair pair)
    {
        First = pair.First;
        Second = pair.Second;
        IsExact = pair.IsExact;
        Similarity = pair.Similarity;
    }

    public PromptItem First { get; }

    public PromptItem Second { get; }

    public bool IsExact { get; }

    public double Similarity { get; }

    public string MatchType => IsExact ? "완전 중복" : "매우 유사";

    public string SimilarityText => $"{Similarity:P0}";

    public string FirstPreview => CreatePreview(First);

    public string SecondPreview => CreatePreview(Second);

    private static string CreatePreview(PromptItem item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.PositivePrompt))
        {
            parts.Add($"+ {item.PositivePrompt}");
        }

        if (!string.IsNullOrWhiteSpace(item.NegativePrompt))
        {
            parts.Add($"- {item.NegativePrompt}");
        }

        var text = string.Join(" / ", parts)
            .Replace(Environment.NewLine, " ")
            .Replace('\n', ' ')
            .Trim();

        return text.Length <= 180
            ? text
            : text[..177] + "...";
    }
}
