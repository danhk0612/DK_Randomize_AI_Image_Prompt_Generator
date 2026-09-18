using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;
using Microsoft.Win32;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class PromptLibraryView : UserControl
{
    private PromptItem? _editingItem;
    private string? _pendingImageSourcePath;
    private bool _removeImage;
    private bool _loaded;
    private bool _suppressSelection;

    public PromptLibraryView()
    {
        ViewModel = new PromptLibraryViewModel(((App)Application.Current).Prompts);
        InitializeComponent();
        DataContext = ViewModel;
        WheelScrollService.Enable(EditorScrollViewer);
        Loaded += PromptLibraryView_Loaded;
        SizeChanged += PromptLibraryView_SizeChanged;
    }

    public PromptLibraryViewModel ViewModel { get; }

    private async void PromptLibraryView_Loaded(object sender, RoutedEventArgs e)
    {
        _loaded = true;
        ApplyResponsiveLayout(ActualWidth);
        await RefreshAsync();
        SetGalleryMode(true);
    }

    private void PromptLibraryView_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyResponsiveLayout(e.NewSize.Width);

    private void ApplyResponsiveLayout(double availableWidth)
    {
        if (availableWidth < 760)
        {
            FilterGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            FilterGrid.ColumnDefinitions[1].Width = new GridLength(0);
            FilterGrid.ColumnDefinitions[2].Width = new GridLength(0);

            Grid.SetRow(SearchFilterPanel, 0);
            Grid.SetColumn(SearchFilterPanel, 0);
            Grid.SetColumnSpan(SearchFilterPanel, 3);

            Grid.SetRow(TagFilterPanel, 1);
            Grid.SetColumn(TagFilterPanel, 0);
            Grid.SetColumnSpan(TagFilterPanel, 3);
            TagFilterPanel.Margin = new Thickness(0, 10, 0, 0);

            ListColumn.MinWidth = 240;
        }
        else
        {
            FilterGrid.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
            FilterGrid.ColumnDefinitions[1].Width = new GridLength(12);
            FilterGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);

            Grid.SetRow(SearchFilterPanel, 0);
            Grid.SetColumn(SearchFilterPanel, 0);
            Grid.SetColumnSpan(SearchFilterPanel, 1);

            Grid.SetRow(TagFilterPanel, 0);
            Grid.SetColumn(TagFilterPanel, 2);
            Grid.SetColumnSpan(TagFilterPanel, 1);
            TagFilterPanel.Margin = new Thickness(0);

            ListColumn.MinWidth = 340;
        }

        if (EditorPane.Visibility == Visibility.Visible)
        {
            EditorColumn.Width = new GridLength(GetEditorWidth(availableWidth));
        }
    }

    private static double GetEditorWidth(double availableWidth) =>
        availableWidth < 820 ? 320 : availableWidth < 1050 ? 350 : 380;

    private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded) return;
        ViewModel.SearchText = SearchBox.Text;
        await RefreshAsync();
    }

    private async void TagFilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_loaded) return;
        ViewModel.TagFilter = TagFilterBox.Text;
        await RefreshAsync();
    }

    private async void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (!_loaded || sender is not RadioButton button || button.Tag is not string tag) return;

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
            ShowError("분류 변경 실패", ex);
        }
    }

    private void GalleryView_Click(object sender, RoutedEventArgs e) => SetGalleryMode(true);
    private void ListView_Click(object sender, RoutedEventArgs e) => SetGalleryMode(false);

    private void SetGalleryMode(bool gallery)
    {
        GalleryListBox.Visibility = gallery ? Visibility.Visible : Visibility.Collapsed;
        DetailListBox.Visibility = gallery ? Visibility.Collapsed : Visibility.Visible;
        GalleryViewButton.FontWeight = gallery ? FontWeights.SemiBold : FontWeights.Normal;
        ListViewButton.FontWeight = gallery ? FontWeights.Normal : FontWeights.SemiBold;
    }

    private void PromptList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelection || sender is not ListBox list || list.SelectedItem is not PromptItem item) return;
        OpenExisting(item);

        _suppressSelection = true;
        list.SelectedItem = null;
        _suppressSelection = false;
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
        OpenEditorAtTop();
    }

    private void OpenExisting(PromptItem item)
    {
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
        OpenEditorAtTop();
    }

    private void OpenEditorAtTop()
    {
        EditorColumn.Width = new GridLength(GetEditorWidth(ActualWidth));
        EditorPane.Visibility = Visibility.Visible;
        EditorScrollViewer.ScrollToTop();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(EditorScrollViewer.ScrollToTop));
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => HideEditor();

    private void HideEditor()
    {
        _editingItem = null;
        _pendingImageSourcePath = null;
        _removeImage = false;
        EditorPane.Visibility = Visibility.Collapsed;
        EditorColumn.Width = new GridLength(0);
        TitleValidationText.Visibility = Visibility.Collapsed;
        ClearImagePreview();
    }

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "대표 이미지 선택",
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.webp;*.bmp|모든 파일|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog() != true) return;

            _pendingImageSourcePath = dialog.FileName;
            _removeImage = false;
            ShowImagePreview(dialog.FileName);
        }
        catch (Exception ex)
        {
            ShowError("이미지 선택 실패", ex);
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
            TitleBox.Focus();
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
                await ViewModel.CreateAsync(TitleBox.Text, PositiveBox.Text, NegativeBox.Text, MemoBox.Text, imagePath, tags);
            }
            else
            {
                await ViewModel.UpdateAsync(_editingItem, TitleBox.Text, PositiveBox.Text, NegativeBox.Text, MemoBox.Text, imagePath, tags);
            }

            if (previousImagePath is not null && previousImagePath != imagePath)
            {
                await app.Images.DeleteIfUnreferencedAsync(previousImagePath);
            }

            StatusText.Text = "프롬프트를 저장했습니다.";
            HideEditor();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            ShowError("프롬프트 저장 실패", ex);
        }
    }

    private async void DuplicatePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null) return;

        try
        {
            var copy = await ViewModel.DuplicateAsync(_editingItem);
            UpdateEmptyState();
            StatusText.Text = "프롬프트를 복제했습니다.";
            OpenExisting(copy);
        }
        catch (Exception ex)
        {
            ShowError("프롬프트 복제 실패", ex);
        }
    }

    private async void DeletePrompt_Click(object sender, RoutedEventArgs e)
    {
        if (_editingItem is null) return;
        var item = _editingItem;

        if (MessageBox.Show(
                $"'{item.Title}' 프롬프트를 삭제합니다.\n최근 기록의 최종 텍스트는 유지됩니다.",
                "프롬프트 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            await ViewModel.DeleteAsync(item);
            await ((App)Application.Current).Images.DeleteIfUnreferencedAsync(item.ImagePath);
            StatusText.Text = "프롬프트를 삭제했습니다.";
            HideEditor();
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            ShowError("프롬프트 삭제 실패", ex);
        }
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
            ShowError("프롬프트 불러오기 실패", ex);
        }
    }

    private void UpdateEmptyState() =>
        EmptyState.Visibility = ViewModel.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void ShowStoredImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            ClearImagePreview();
            return;
        }

        try
        {
            ShowImagePreview(((App)Application.Current).Images.ResolvePath(relativePath));
        }
        catch
        {
            ClearImagePreview();
        }
    }

    private void ShowImagePreview(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();
        EditorImagePreview.Source = bitmap;
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

    private void ShowError(string title, Exception ex)
    {
        StatusText.Text = $"{title}: {ex.Message}";
        MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static IReadOnlyList<string> ParseTags(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
