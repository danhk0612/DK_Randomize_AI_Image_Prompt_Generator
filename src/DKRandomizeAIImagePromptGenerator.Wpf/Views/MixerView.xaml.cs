using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class MixerView : UserControl
{
    private bool _initialized;
    private bool _syncing;

    public MixerView()
    {
        var app = (App)Application.Current;
        ViewModel = new MixerViewModel(app.Prompts, app.History, app.Combination);
        InitializeComponent();
        DataContext = ViewModel;

        CharacterPromptCombo.ItemsSource = ViewModel.CharacterItems;
        ArtistPromptCombo.ItemsSource = ViewModel.ArtistItems;
        AdditionalPromptCombo.ItemsSource = ViewModel.AdditionalItems;

        WheelScrollService.Enable(RootScrollViewer);
        Loaded += MixerView_Loaded;
        _initialized = true;
    }

    public MixerViewModel ViewModel { get; }

    private async void MixerView_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadAsync();
        var app = (App)Application.Current;
        if (app.PendingHistoryRestore is CombinationHistory history)
        {
            ViewModel.RestoreFromHistory(history);
            app.PendingHistoryRestore = null;
        }

        SyncControls();
    }

    private void CharacterModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetMode(PromptCategory.Character, CharacterModeCombo.SelectedIndex);
    private void ArtistModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetMode(PromptCategory.Artist, ArtistModeCombo.SelectedIndex);
    private void AdditionalModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetMode(PromptCategory.Additional, AdditionalModeCombo.SelectedIndex);
    private void CharacterPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetSelected(PromptCategory.Character, CharacterPromptCombo.SelectedItem as PromptItem);
    private void ArtistPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetSelected(PromptCategory.Artist, ArtistPromptCombo.SelectedItem as PromptItem);
    private void AdditionalPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) => SetSelected(PromptCategory.Additional, AdditionalPromptCombo.SelectedItem as PromptItem);

    private void CharacterRandom_Click(object sender, RoutedEventArgs e) { ViewModel.RandomizeCategory(PromptCategory.Character); SyncControls(); }
    private void ArtistRandom_Click(object sender, RoutedEventArgs e) { ViewModel.RandomizeCategory(PromptCategory.Artist); SyncControls(); }
    private void AdditionalRandom_Click(object sender, RoutedEventArgs e) { ViewModel.RandomizeCategory(PromptCategory.Additional); SyncControls(); }
    private void RandomizeAll_Click(object sender, RoutedEventArgs e) { ViewModel.RandomizeAll(); SyncControls(); }

    private void CopyPositive_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(PositiveOutput.Text ?? string.Empty);
        StatusText.Text = "Positive 프롬프트를 복사했습니다.";
    }

    private void CopyNegative_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(NegativeOutput.Text ?? string.Empty);
        StatusText.Text = "Negative 프롬프트를 복사했습니다.";
    }

    private async void SaveHistory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ViewModel.SaveHistoryAsync(PositiveOutput.Text ?? string.Empty, NegativeOutput.Text ?? string.Empty);
            StatusText.Text = "최근 기록에 저장했습니다.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"최근 기록 저장 실패: {ex.Message}";
        }
    }

    private void SetMode(PromptCategory category, int selectedIndex)
    {
        if (!_initialized || _syncing || selectedIndex < 0) return;
        ViewModel.SetMode(category, (PromptSelectionMode)selectedIndex);
        SyncControls();
    }

    private void SetSelected(PromptCategory category, PromptItem? item)
    {
        if (!_initialized || _syncing) return;
        ViewModel.SetSelectedItem(category, item);
        SyncControls();
    }

    private void SyncControls()
    {
        _syncing = true;
        try
        {
            CharacterModeCombo.SelectedIndex = (int)ViewModel.CharacterMode;
            ArtistModeCombo.SelectedIndex = (int)ViewModel.ArtistMode;
            AdditionalModeCombo.SelectedIndex = (int)ViewModel.AdditionalMode;

            CharacterPromptCombo.SelectedItem = ViewModel.SelectedCharacter;
            ArtistPromptCombo.SelectedItem = ViewModel.SelectedArtist;
            AdditionalPromptCombo.SelectedItem = ViewModel.SelectedAdditional;

            CharacterPromptCombo.IsEnabled = ViewModel.CharacterMode == PromptSelectionMode.Fixed;
            ArtistPromptCombo.IsEnabled = ViewModel.ArtistMode == PromptSelectionMode.Fixed;
            AdditionalPromptCombo.IsEnabled = ViewModel.AdditionalMode == PromptSelectionMode.Fixed;

            CharacterRandomButton.IsEnabled = ViewModel.CharacterMode == PromptSelectionMode.Random;
            ArtistRandomButton.IsEnabled = ViewModel.ArtistMode == PromptSelectionMode.Random;
            AdditionalRandomButton.IsEnabled = ViewModel.AdditionalMode == PromptSelectionMode.Random;

            CharacterTitle.Text = ViewModel.SelectedCharacter?.Title ?? "선택된 캐릭터 없음";
            ArtistTitle.Text = ViewModel.SelectedArtist?.Title ?? "선택된 작가 없음";
            AdditionalTitle.Text = ViewModel.SelectedAdditional?.Title ?? "선택된 추가 프롬프트 없음";
            CharacterMemo.Text = ViewModel.SelectedCharacter?.Memo ?? string.Empty;
            ArtistMemo.Text = ViewModel.SelectedArtist?.Memo ?? string.Empty;
            AdditionalMemo.Text = ViewModel.SelectedAdditional?.Memo ?? string.Empty;
            CharacterTags.ItemsSource = ViewModel.SelectedCharacter?.Tags;
            ArtistTags.ItemsSource = ViewModel.SelectedArtist?.Tags;
            AdditionalTags.ItemsSource = ViewModel.SelectedAdditional?.Tags;

            SetImage(CharacterImage, ViewModel.SelectedCharacter);
            SetImage(ArtistImage, ViewModel.SelectedArtist);
            SetImage(AdditionalImage, ViewModel.SelectedAdditional);

            PositiveOutput.Text = ViewModel.PositiveText;
            NegativeOutput.Text = ViewModel.NegativeText;
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void SetImage(Image image, PromptItem? item)
    {
        image.Source = null;
        if (item?.ImagePath is not string relativePath || string.IsNullOrWhiteSpace(relativePath)) return;

        try
        {
            var app = (App)Application.Current;
            var fullPath = app.Images.ResolvePath(relativePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            image.Source = bitmap;
        }
        catch
        {
            image.Source = null;
        }
    }
}
