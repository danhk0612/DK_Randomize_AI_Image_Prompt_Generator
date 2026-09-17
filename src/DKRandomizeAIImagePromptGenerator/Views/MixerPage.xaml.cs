using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DKRandomizeAIImagePromptGenerator.Views;

public sealed partial class MixerPage : Page
{
    private bool _initialized;
    private bool _syncingControls;

    public MixerPage()
    {
        var app = (App)Application.Current;
        ViewModel = new MixerViewModel(app.Prompts, app.History, app.Combination);
        InitializeComponent();
        _initialized = true;
        Loaded += MixerPage_Loaded;
        SizeChanged += MixerPage_SizeChanged;
    }

    public MixerViewModel ViewModel { get; }

    private async void MixerPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveCardLayout(ActualWidth);

        var app = (App)Application.Current;
        await ViewModel.LoadAsync();

        if (app.PendingHistoryRestore is CombinationHistory history)
        {
            ViewModel.RestoreFromHistory(history);
            app.PendingHistoryRestore = null;
        }

        SyncControls();
    }

    private void MixerPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveCardLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveCardLayout(double availableWidth)
    {
        var characterCard = FindCardBorder(CharacterImage);
        var artistCard = FindCardBorder(ArtistImage);
        var additionalCard = FindCardBorder(AdditionalImage);

        if (characterCard?.Parent is not Grid cardGrid ||
            artistCard is null ||
            additionalCard is null ||
            cardGrid.ColumnDefinitions.Count < 3)
        {
            return;
        }

        EnsureCardRows(cardGrid);

        Grid.SetColumnSpan(characterCard, 1);
        Grid.SetColumnSpan(artistCard, 1);
        Grid.SetColumnSpan(additionalCard, 1);

        if (availableWidth >= 1100)
        {
            cardGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.RowSpacing = 0;

            PositionCard(characterCard, 0, 0);
            PositionCard(artistCard, 0, 1);
            PositionCard(additionalCard, 0, 2);
        }
        else if (availableWidth >= 760)
        {
            cardGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.ColumnDefinitions[2].Width = new GridLength(0);
            cardGrid.RowSpacing = 12;

            PositionCard(characterCard, 0, 0);
            PositionCard(artistCard, 0, 1);
            PositionCard(additionalCard, 1, 0);
            Grid.SetColumnSpan(additionalCard, 2);
        }
        else
        {
            cardGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            cardGrid.ColumnDefinitions[1].Width = new GridLength(0);
            cardGrid.ColumnDefinitions[2].Width = new GridLength(0);
            cardGrid.RowSpacing = 12;

            PositionCard(characterCard, 0, 0);
            PositionCard(artistCard, 1, 0);
            PositionCard(additionalCard, 2, 0);
        }
    }

    private static void EnsureCardRows(Grid grid)
    {
        while (grid.RowDefinitions.Count < 3)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }
    }

    private static void PositionCard(FrameworkElement card, int row, int column)
    {
        Grid.SetRow(card, row);
        Grid.SetColumn(card, column);
    }

    private static Border? FindCardBorder(DependencyObject start)
    {
        DependencyObject? current = start;
        while (current is not null)
        {
            if (current is Border border && VisualTreeHelper.GetParent(border) is Grid)
            {
                return border;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private void CharacterModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetMode(PromptCategory.Character, CharacterModeCombo.SelectedIndex);

    private void ArtistModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetMode(PromptCategory.Artist, ArtistModeCombo.SelectedIndex);

    private void AdditionalModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetMode(PromptCategory.Additional, AdditionalModeCombo.SelectedIndex);

    private void CharacterPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetSelected(PromptCategory.Character, CharacterPromptCombo.SelectedItem as PromptItem);

    private void ArtistPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetSelected(PromptCategory.Artist, ArtistPromptCombo.SelectedItem as PromptItem);

    private void AdditionalPromptCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        SetSelected(PromptCategory.Additional, AdditionalPromptCombo.SelectedItem as PromptItem);

    private void CharacterRandom_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RandomizeCategory(PromptCategory.Character);
        SyncControls();
    }

    private void ArtistRandom_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RandomizeCategory(PromptCategory.Artist);
        SyncControls();
    }

    private void AdditionalRandom_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RandomizeCategory(PromptCategory.Additional);
        SyncControls();
    }

    private void RandomizeAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RandomizeAll();
        SyncControls();
    }

    private void CopyPositive_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).Clipboard.CopyText(PositiveOutput.Text);
    }

    private void CopyNegative_Click(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).Clipboard.CopyText(NegativeOutput.Text);
    }

    private async void SaveHistory_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveHistoryAsync(PositiveOutput.Text, NegativeOutput.Text);
        HistorySavedInfoBar.IsOpen = true;
    }

    private void SetMode(PromptCategory category, int selectedIndex)
    {
        if (!_initialized || _syncingControls || selectedIndex < 0)
        {
            return;
        }

        ViewModel.SetMode(category, (PromptSelectionMode)selectedIndex);
        SyncControls();
    }

    private void SetSelected(PromptCategory category, PromptItem? item)
    {
        if (!_initialized || _syncingControls)
        {
            return;
        }

        ViewModel.SetSelectedItem(category, item);
        SyncControls();
    }

    private void SyncControls()
    {
        if (!_initialized)
        {
            return;
        }

        _syncingControls = true;
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

            SetImage(CharacterImage, ViewModel.SelectedCharacter);
            SetImage(ArtistImage, ViewModel.SelectedArtist);
            SetImage(AdditionalImage, ViewModel.SelectedAdditional);

            PositiveOutput.Text = ViewModel.PositiveText;
            NegativeOutput.Text = ViewModel.NegativeText;
        }
        finally
        {
            _syncingControls = false;
        }
    }

    private static void SetImage(Image image, PromptItem? item)
    {
        if (item?.ImagePath is not string relativePath || string.IsNullOrWhiteSpace(relativePath))
        {
            image.Source = null;
            return;
        }

        var fullPath = ((App)Application.Current).Images.ResolvePath(relativePath);
        image.Source = new BitmapImage(new Uri(fullPath));
    }
}
