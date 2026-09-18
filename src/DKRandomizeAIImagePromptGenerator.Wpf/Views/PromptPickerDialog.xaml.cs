using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class PromptPickerDialog : Window
{
    private readonly PromptCategory _category;
    private readonly IReadOnlyList<PromptItem> _allItems;
    private readonly ObservableCollection<PromptItem> _filteredItems = [];
    private readonly ObservableCollection<PromptItem> _selectedItems = [];

    public PromptPickerDialog(
        PromptCategory category,
        IReadOnlyList<PromptItem> availableItems,
        IReadOnlyList<PromptItem> selectedItems)
    {
        _category = category;
        _allItems = availableItems
            .Where(item => item.Category == category)
            .ToArray();

        InitializeComponent();

        HeadingText.Text = $"{CategoryLabel(category)} 프롬프트 선택";
        SearchResultsList.ItemsSource = _filteredItems;
        SelectedList.ItemsSource = _selectedItems;

        foreach (var item in selectedItems.Where(item => item.Category == category))
        {
            if (_selectedItems.All(existing => existing.Id != item.Id))
            {
                _selectedItems.Add(item);
            }
        }

        RefreshSearchResults();
        UpdateStatus();
        Loaded += (_, _) => SearchBox.Focus();
    }

    public IReadOnlyList<PromptItem> SelectedItems =>
        _selectedItems.ToArray();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) =>
        RefreshSearchResults();

    private void RefreshSearchResults()
    {
        var query = SearchBox.Text?.Trim() ?? string.Empty;
        var matches = string.IsNullOrWhiteSpace(query)
            ? _allItems
            : _allItems.Where(item => Matches(item, query)).ToArray();

        _filteredItems.Clear();
        foreach (var item in matches)
        {
            _filteredItems.Add(item);
        }

        SearchResultTitle.Text = $"검색 결과 ({_filteredItems.Count})";
        UpdateStatus();
    }

    private static bool Matches(PromptItem item, string query)
    {
        return Contains(item.Title, query) ||
               Contains(item.Memo, query) ||
               Contains(item.PositivePrompt, query) ||
               Contains(item.NegativePrompt, query) ||
               item.Tags.Any(tag => Contains(tag, query));
    }

    private static bool Contains(string? text, string query) =>
        !string.IsNullOrWhiteSpace(text) &&
        text.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private void AddSelected_Click(object sender, RoutedEventArgs e) =>
        AddSearchSelection();

    private void SearchResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) =>
        AddSearchSelection();

    private void AddSearchSelection()
    {
        var additions = SearchResultsList.SelectedItems
            .OfType<PromptItem>()
            .ToArray();

        if (additions.Length == 0 && SearchResultsList.SelectedItem is PromptItem single)
        {
            additions = [single];
        }

        foreach (var item in additions)
        {
            if (_selectedItems.All(existing => existing.Id != item.Id))
            {
                _selectedItems.Add(item);
            }
        }

        UpdateStatus();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        var removals = SelectedList.SelectedItems
            .OfType<PromptItem>()
            .ToArray();

        foreach (var item in removals)
        {
            _selectedItems.Remove(item);
        }

        UpdateStatus();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedList.SelectedItem is not PromptItem item)
        {
            return;
        }

        var index = _selectedItems.IndexOf(item);
        if (index <= 0)
        {
            return;
        }

        _selectedItems.Move(index, index - 1);
        SelectedList.SelectedItem = item;
        SelectedList.ScrollIntoView(item);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedList.SelectedItem is not PromptItem item)
        {
            return;
        }

        var index = _selectedItems.IndexOf(item);
        if (index < 0 || index >= _selectedItems.Count - 1)
        {
            return;
        }

        _selectedItems.Move(index, index + 1);
        SelectedList.SelectedItem = item;
        SelectedList.ScrollIntoView(item);
    }

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

    private void UpdateStatus()
    {
        SelectedTitle.Text = $"선택됨 ({_selectedItems.Count})";
        StatusText.Text = $"{CategoryLabel(_category)} {_selectedItems.Count}개 선택";
    }

    private static string CategoryLabel(PromptCategory category) => category switch
    {
        PromptCategory.Character => "캐릭터",
        PromptCategory.Artist => "작가 / 스타일",
        PromptCategory.Additional => "추가",
        _ => "프롬프트"
    };
}
