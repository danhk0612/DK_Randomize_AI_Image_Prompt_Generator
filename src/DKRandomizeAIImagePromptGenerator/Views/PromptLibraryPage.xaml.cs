using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DKRandomizeAIImagePromptGenerator.Views;

public sealed partial class PromptLibraryPage : Page
{
    private PromptItem? _editingItem;

    public PromptLibraryPage()
    {
        ViewModel = new PromptLibraryViewModel(((App)Application.Current).Prompts);
        InitializeComponent();
        Loaded += PromptLibraryPage_Loaded;
    }

    public PromptLibraryViewModel ViewModel { get; }

    private async void PromptLibraryPage_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ViewModel.SearchText = SearchBox.Text;
        await RefreshAsync();
    }

    private async void TagFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ViewModel.TagFilter = TagFilterBox.Text;
        await RefreshAsync();
    }

    private async void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || sender is not RadioButton button || button.Tag is not string tag)
        {
            return;
        }

        var category = tag switch
        {
            "Artist" => PromptCategory.Artist,
            "Additional" => PromptCategory.Additional,
            _ => PromptCategory.Character
        };

        HideEditor();
        await ViewModel.SetCategoryAsync(category);
        UpdateEmptyState();
    }

    private void NewPrompt_Click(object sender, RoutedEventArgs e)
    {
        _editingItem = null;
        EditorTitleText.Text = "새 프롬프트";
        TitleBox.Text = string.Empty;
        PositiveBox.Text = string.Empty;
        NegativeBox.Text = string.Empty;
        TagsBox.Text = string.Empty;
        MemoBox.Text = string.Empty;
        TitleValidationText.Visibility = Visibility.Collapsed;
        DuplicateButton.Visibility = Visibility.Collapsed;
        DeleteButton.Visibility = Visibility.Collapsed;
        EditorPane.Visibility = Visibility.Visible;
        TitleBox.Focus(FocusState.Programmatic);
    }

    private void PromptGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not PromptItem item)
        {
            return;
        }

        _editingItem = item;
        EditorTitleText.Text = "프롬프트 편집";
        TitleBox.Text = item.Title;
        PositiveBox.Text = item.PositivePrompt;
        NegativeBox.Text = item.NegativePrompt;
        TagsBox.Text = string.Join(", ", item.Tags);
        MemoBox.Text = item.Memo;
        TitleValidationText.Visibility = Visibility.Collapsed;
        DuplicateButton.Visibility = Visibility.Visible;
        DeleteButton.Visibility = Visibility.Visible;
        EditorPane.Visibility = Visibility.Visible;
    }

    private async void SavePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            TitleValidationText.Visibility = Visibility.Visible;
            TitleBox.Focus(FocusState.Programmatic);
            return;
        }

        var tags = ParseTags(TagsBox.Text);

        if (_editingItem is null)
        {
            await ViewModel.CreateAsync(
                TitleBox.Text,
                PositiveBox.Text,
                NegativeBox.Text,
                MemoBox.Text,
                imagePath: null,
                tags);
        }
        else
        {
            await ViewModel.UpdateAsync(
                _editingItem,
                TitleBox.Text,
                PositiveBox.Text,
                NegativeBox.Text,
                MemoBox.Text,
                _editingItem.ImagePath,
                tags);
        }

        HideEditor();
        UpdateEmptyState();
    }

    private async void DuplicatePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null)
        {
            return;
        }

        var copy = await ViewModel.DuplicateAsync(_editingItem);
        _editingItem = copy;
        EditorTitleText.Text = "프롬프트 편집";
        TitleBox.Text = copy.Title;
        PositiveBox.Text = copy.PositivePrompt;
        NegativeBox.Text = copy.NegativePrompt;
        TagsBox.Text = string.Join(", ", copy.Tags);
        MemoBox.Text = copy.Memo;
        UpdateEmptyState();
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "프롬프트 삭제",
            Content = $"'{_editingItem.Title}' 프롬프트를 삭제합니다. 최근 기록의 최종 텍스트는 유지됩니다.",
            PrimaryButtonText = "삭제",
            CloseButtonText = "취소",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeleteAsync(_editingItem);
        HideEditor();
        UpdateEmptyState();
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        HideEditor();
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

    private void HideEditor()
    {
        _editingItem = null;
        EditorPane.Visibility = Visibility.Collapsed;
        TitleValidationText.Visibility = Visibility.Collapsed;
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
