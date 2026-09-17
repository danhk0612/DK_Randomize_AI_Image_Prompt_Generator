using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.Storage.Pickers;

namespace DKRandomizeAIImagePromptGenerator.Views;

public sealed partial class PromptLibraryPage : Page
{
    private PromptItem? _editingItem;
    private string? _pendingImageSourcePath;
    private bool _removeImage;

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
        _pendingImageSourcePath = null;
        _removeImage = false;
        EditorTitleText.Text = "새 프롬프트";
        TitleBox.Text = string.Empty;
        PositiveBox.Text = string.Empty;
        NegativeBox.Text = string.Empty;
        TagsBox.Text = string.Empty;
        MemoBox.Text = string.Empty;
        TitleValidationText.Visibility = Visibility.Collapsed;
        DuplicateButton.Visibility = Visibility.Collapsed;
        DeleteButton.Visibility = Visibility.Collapsed;
        ClearImagePreview();
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
        _pendingImageSourcePath = null;
        _removeImage = false;
        EditorTitleText.Text = "프롬프트 편집";
        TitleBox.Text = item.Title;
        PositiveBox.Text = item.PositivePrompt;
        NegativeBox.Text = item.NegativePrompt;
        TagsBox.Text = string.Join(", ", item.Tags);
        MemoBox.Text = item.Memo;
        TitleValidationText.Visibility = Visibility.Collapsed;
        DuplicateButton.Visibility = Visibility.Visible;
        DeleteButton.Visibility = Visibility.Visible;
        ShowStoredImage(item.ImagePath);
        EditorPane.Visibility = Visibility.Visible;
    }

    private async void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var window = ((App)Application.Current).MainWindowInstance;
        if (window is null)
        {
            return;
        }

        var picker = new FileOpenPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            CommitButtonText = "선택",
            ViewMode = PickerViewMode.Thumbnail,
            FileTypeFilter = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" }
        };

        var result = await picker.PickSingleFileAsync();
        if (result is null)
        {
            return;
        }

        _pendingImageSourcePath = result.Path;
        _removeImage = false;
        ShowImagePreview(result.Path);
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        _pendingImageSourcePath = null;
        _removeImage = true;
        ClearImagePreview();
    }

    private async void SavePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            TitleValidationText.Visibility = Visibility.Visible;
            TitleBox.Focus(FocusState.Programmatic);
            return;
        }

        var app = (App)Application.Current;
        var previousImagePath = _editingItem?.ImagePath;
        var imagePath = previousImagePath;

        if (_pendingImageSourcePath is not null)
        {
            imagePath = await app.Images.ImportAsync(_pendingImageSourcePath);
        }
        else if (_removeImage)
        {
            imagePath = null;
        }

        var tags = ParseTags(TagsBox.Text);

        if (_editingItem is null)
        {
            await ViewModel.CreateAsync(
                TitleBox.Text,
                PositiveBox.Text,
                NegativeBox.Text,
                MemoBox.Text,
                imagePath,
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
                imagePath,
                tags);
        }

        if (previousImagePath is not null && previousImagePath != imagePath)
        {
            await app.Images.DeleteIfUnreferencedAsync(previousImagePath);
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
        _pendingImageSourcePath = null;
        _removeImage = false;
        EditorTitleText.Text = "프롬프트 편집";
        TitleBox.Text = copy.Title;
        PositiveBox.Text = copy.PositivePrompt;
        NegativeBox.Text = copy.NegativePrompt;
        TagsBox.Text = string.Join(", ", copy.Tags);
        MemoBox.Text = copy.Memo;
        ShowStoredImage(copy.ImagePath);
        UpdateEmptyState();
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null)
        {
            return;
        }

        var item = _editingItem;
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "프롬프트 삭제",
            Content = $"'{item.Title}' 프롬프트를 삭제합니다. 최근 기록의 최종 텍스트는 유지됩니다.",
            PrimaryButtonText = "삭제",
            CloseButtonText = "취소",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await ViewModel.DeleteAsync(item);
        await ((App)Application.Current).Images.DeleteIfUnreferencedAsync(item.ImagePath);
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
        _pendingImageSourcePath = null;
        _removeImage = false;
        EditorPane.Visibility = Visibility.Collapsed;
        TitleValidationText.Visibility = Visibility.Collapsed;
        ClearImagePreview();
    }

    private void ShowStoredImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            ClearImagePreview();
            return;
        }

        var fullPath = ((App)Application.Current).Images.ResolvePath(relativePath);
        ShowImagePreview(fullPath);
    }

    private void ShowImagePreview(string path)
    {
        EditorImagePreview.Source = new BitmapImage(new Uri(path));
        EditorImagePreview.Visibility = Visibility.Visible;
        EditorImagePlaceholder.Visibility = Visibility.Collapsed;
        RemoveImageButton.IsEnabled = true;
    }

    private void ClearImagePreview()
    {
        EditorImagePreview.Source = null;
        EditorImagePreview.Visibility = Visibility.Collapsed;
        EditorImagePlaceholder.Visibility = Visibility.Visible;
        RemoveImageButton.IsEnabled = false;
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
