using System.Windows;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class RandomTagPickerDialog : Window
{
    private readonly IReadOnlyList<string> _allTags;

    public RandomTagPickerDialog(
        PromptCategory category,
        IReadOnlyList<string> availableTags,
        IReadOnlyList<string> selectedTags)
    {
        InitializeComponent();

        _allTags = availableTags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        HeadingText.Text = $"{CategoryLabel(category)} 랜덤 태그 조건";
        TagList.ItemsSource = _allTags;

        var selected = new HashSet<string>(
            selectedTags,
            StringComparer.OrdinalIgnoreCase);

        foreach (var tag in _allTags.Where(selected.Contains))
        {
            TagList.SelectedItems.Add(tag);
        }

        UpdateCount();
        TagList.SelectionChanged += (_, _) => UpdateCount();
    }

    public IReadOnlyList<string> SelectedTags =>
        TagList.SelectedItems
            .OfType<string>()
            .ToArray();

    private void Clear_Click(object sender, RoutedEventArgs e) =>
        TagList.UnselectAll();

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateCount()
    {
        CountText.Text = TagList.SelectedItems.Count == 0
            ? $"태그 {_allTags.Count}개 · 조건 없음"
            : $"태그 {_allTags.Count}개 · {TagList.SelectedItems.Count}개 AND 조건";
    }

    private static string CategoryLabel(PromptCategory category) => category switch
    {
        PromptCategory.Character => "캐릭터",
        PromptCategory.Artist => "작가 / 스타일",
        PromptCategory.Additional => "추가",
        _ => "프롬프트"
    };
}