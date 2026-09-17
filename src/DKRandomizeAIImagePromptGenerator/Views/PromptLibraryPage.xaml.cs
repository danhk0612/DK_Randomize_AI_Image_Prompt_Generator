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
    private bool _syncingViewMode;

    public PromptLibraryPage()
    {
        ViewModel = new PromptLibraryViewModel(((App)Application.Current).Prompts);
        InitializeComponent();
        Loaded += PromptLibraryPage_Loaded;
        SizeChanged += PromptLibraryPage_SizeChanged;
    }

    public PromptLibraryViewModel ViewModel { get; }

    private async void PromptLibraryPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveLayout(ActualWidth);
        await RefreshAsync();
    }

    private void PromptLibraryPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double availableWidth)
    {
        if (Content is Grid rootGrid)
        {
            rootGrid.MaxWidth = double.PositiveInfinity;
            rootGrid.Padding = new Thickness(24);
        }

        if (EditorPane.Parent is Grid contentGrid && contentGrid.ColumnDefinitions.Count > 0)
        {
            contentGrid.ColumnDefinitions[0].MinWidth = 0;
        }

        if (SearchBox.Parent is Grid filterGrid && filterGrid.ColumnDefinitions.Count >= 2)
        {
            if (filterGrid.RowDefinitions.Count < 2)
            {
                filterGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                filterGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            if (availableWidth < 760)
            {
                filterGrid.ColumnSpacing = 0;
                filterGrid.RowSpacing = 10;
                filterGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                filterGrid.ColumnDefinitions[1].Width = new GridLength(0);

                Grid.SetRow(SearchBox, 0);
                Grid.SetColumn(SearchBox, 0);
                Grid.SetColumnSpan(SearchBox, 2);

                Grid.SetRow(TagFilterBox, 1);
                Grid.SetColumn(TagFilterBox, 0);
                Grid.SetColumnSpan(TagFilterBox, 2);
            }
            else
            {
                filterGrid.ColumnSpacing = 10;
                filterGrid.RowSpacing = 0;
                filterGrid.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
                filterGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                Grid.SetRow(SearchBox, 0);
                Grid.SetColumn(SearchBox, 0);
                Grid.SetColumnSpan(SearchBox, 1);

                Grid.SetRow(TagFilterBox, 0);
                Grid.SetColumn(TagFilterBox, 1);
                Grid.SetColumnSpan(TagFilterBox, 1);
            }
        }

        EditorPane.Width = availableWidth switch
        {
            >= 1350 => 400,
            >= 1100 => 340,
            >= 900 => 300,
            _ => 260
        };
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

        try
        {
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
        catch (Exception ex)
        {
            await ShowErrorAsync("분류 변경 실패", ex);
        }
    }

    private void GalleryView_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _syncingViewMode)
        {
            return;
        }

        _syncingViewMode = true;
        GalleryViewButton.IsChecked = true;
        ListViewButton.IsChecked = false;
        PromptGridView.Visibility = Visibility.Visible;
        PromptListView.Visibility = Visibility.Collapsed;
        _syncingViewMode = false;
    }

    private void ListView_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded || _syncingViewMode)
        {
            return;
        }

        _syncingViewMode = true;
        GalleryViewButton.IsChecked = false;
        ListViewButton.IsChecked = true;
        PromptGridView.Visibility = Visibility.Collapsed;
        PromptListView.Visibility = Visibility.Visible;
        _syncingViewMode = false;
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
        ResetEditorScroll();
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
        ResetEditorScroll();
    }

    private async void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var window = ((App)Application.Current).MainWindowInstance;
        if (window is null)
        {
            return;
        }

        try
        {
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
        catch (Exception ex)
        {
            await ShowErrorAsync("이미지 선택 실패", ex);
        }
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

        try
        {
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
        catch (Exception ex)
        {
            await ShowErrorAsync("프롬프트 저장 실패", ex);
        }
    }

    private async void DuplicatePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null)
        {
            return;
        }

        try
        {
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
            ResetEditorScroll();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("프롬프트 복제 실패", ex);
        }
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null)
        {
            return;
        }

        try
        {
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
        catch (Exception ex)
        {
            await ShowErrorAsync("프롬프트 삭제 실패", ex);
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        HideEditor();
    }

    private async Task RefreshAsync()
    {
        try
        {
            await ViewModel.RefreshAsync();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync("프롬프트 불러오기 실패", ex);
        }
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
        ResetEditorScroll();
        EditorPane.Visibility = Visibility.Collapsed;
        TitleValidationText.Visibility = Visibility.Collapsed;
        ClearImagePreview();
    }

    private void ResetEditorScroll()
    {
        if (EditorPane.Child is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.ChangeView(null, 0, null, disableAnimation: true);
        DispatcherQueue.TryEnqueue(() =>
            scrollViewer.ChangeView(null, 0, null, disableAnimation: true));
    }

    private void ShowStoredImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            ClearImagePreview();
            return;
        }

        try
        {
            var fullPath = ((App)Application.Current).Images.ResolvePath(relativePath);
            ShowImagePreview(fullPath);
        }
        catch
        {
            ClearImagePreview();
        }
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

    private async Task ShowErrorAsync(string title, Exception exception)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = title,
            Content = string.IsNullOrWhiteSpace(exception.Message)
                ? "작업 중 알 수 없는 오류가 발생했습니다."
                : exception.Message,
            CloseButtonText = "확인",
            DefaultButton = ContentDialogButton.Close
        };

        await dialog.ShowAsync();
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
