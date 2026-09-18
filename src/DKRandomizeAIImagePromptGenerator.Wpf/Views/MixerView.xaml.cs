using System.Windows;
using System.Windows.Controls;
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

        CharacterSelectedList.ItemsSource = ViewModel.SelectedCharacters;
        ArtistSelectedList.ItemsSource = ViewModel.SelectedArtists;
        AdditionalSelectedList.ItemsSource = ViewModel.SelectedAdditionals;

        WheelScrollService.Enable(RootScrollViewer);
        Loaded += MixerView_Loaded;
        SizeChanged += MixerView_SizeChanged;
        _initialized = true;
    }

    public MixerViewModel ViewModel { get; }

    private async void MixerView_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveCardLayout(ActualWidth);
        await ViewModel.LoadAsync();

        var app = (App)Application.Current;
        if (app.PendingHistoryRestore is CombinationHistory history)
        {
            ViewModel.RestoreFromHistory(history);
            app.PendingHistoryRestore = null;
        }

        SyncControls();
    }

    private void MixerView_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyResponsiveCardLayout(e.NewSize.Width);

    private void ApplyResponsiveCardLayout(double availableWidth)
    {
        Grid.SetColumnSpan(CharacterCard, 1);
        Grid.SetColumnSpan(ArtistCard, 1);
        Grid.SetColumnSpan(AdditionalCard, 1);

        CharacterCard.Margin = new Thickness(0);
        ArtistCard.Margin = new Thickness(0);
        AdditionalCard.Margin = new Thickness(0);

        if (availableWidth >= 1100)
        {
            CardsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            CardsGrid.ColumnDefinitions[1].Width = new GridLength(12);
            CardsGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            CardsGrid.ColumnDefinitions[3].Width = new GridLength(12);
            CardsGrid.ColumnDefinitions[4].Width = new GridLength(1, GridUnitType.Star);

            PositionCard(CharacterCard, 0, 0);
            PositionCard(ArtistCard, 0, 2);
            PositionCard(AdditionalCard, 0, 4);
        }
        else if (availableWidth >= 760)
        {
            CardsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            CardsGrid.ColumnDefinitions[1].Width = new GridLength(12);
            CardsGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
            CardsGrid.ColumnDefinitions[3].Width = new GridLength(0);
            CardsGrid.ColumnDefinitions[4].Width = new GridLength(0);

            PositionCard(CharacterCard, 0, 0);
            PositionCard(ArtistCard, 0, 2);
            PositionCard(AdditionalCard, 1, 0);
            Grid.SetColumnSpan(AdditionalCard, 3);
            AdditionalCard.Margin = new Thickness(0, 12, 0, 0);
        }
        else
        {
            CardsGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            for (var index = 1; index < CardsGrid.ColumnDefinitions.Count; index++)
            {
                CardsGrid.ColumnDefinitions[index].Width = new GridLength(0);
            }

            PositionCard(CharacterCard, 0, 0);
            PositionCard(ArtistCard, 1, 0);
            PositionCard(AdditionalCard, 2, 0);
            ArtistCard.Margin = new Thickness(0, 12, 0, 0);
            AdditionalCard.Margin = new Thickness(0, 12, 0, 0);
        }
    }

    private static void PositionCard(FrameworkElement card, int row, int column)
    {
        Grid.SetRow(card, row);
        Grid.SetColumn(card, column);
    }

    private void ModeRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized || _syncing ||
            sender is not FrameworkElement element ||
            element.Tag is not string tag)
        {
            return;
        }

        var parts = tag.Split(':', 2);
        if (parts.Length != 2 ||
            !Enum.TryParse<PromptCategory>(parts[0], out var category) ||
            !Enum.TryParse<PromptSelectionMode>(parts[1], out var mode))
        {
            return;
        }

        ViewModel.SetMode(category, mode);
        SyncControls();
    }

    private void RandomCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _syncing ||
            sender is not ComboBox combo ||
            combo.Tag is not string tag ||
            !Enum.TryParse<PromptCategory>(tag, out var category) ||
            combo.SelectedItem is not int count)
        {
            return;
        }

        ViewModel.SetRandomCount(category, count);
        SyncControls();
    }

    private void OpenPicker_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetCategory(sender, out var category) ||
            ViewModel.GetMode(category) != PromptSelectionMode.Fixed)
        {
            return;
        }

        var dialog = new PromptPickerDialog(
            category,
            ViewModel.GetAvailableItems(category),
            ViewModel.GetSelectedItems(category))
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        ViewModel.SetSelectedItems(category, dialog.SelectedItems);
        SyncControls();
    }

    private void RemoveSelected_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetCategory(sender, out var category) ||
            ViewModel.GetMode(category) != PromptSelectionMode.Fixed)
        {
            return;
        }

        var list = GetSelectedList(category);
        if (list.SelectedItem is not PromptItem item)
        {
            return;
        }

        ViewModel.RemoveSelectedItem(category, item.Id);
        SyncControls();
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e) =>
        MoveSelection(sender, -1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) =>
        MoveSelection(sender, 1);

    private void MoveSelection(object sender, int offset)
    {
        if (!TryGetCategory(sender, out var category) ||
            ViewModel.GetMode(category) != PromptSelectionMode.Fixed)
        {
            return;
        }

        var list = GetSelectedList(category);
        if (list.SelectedItem is not PromptItem item || list.SelectedIndex < 0)
        {
            return;
        }

        var targetIndex = list.SelectedIndex + offset;
        if (ViewModel.MoveSelectedItem(category, list.SelectedIndex, targetIndex))
        {
            list.SelectedItem = item;
            list.ScrollIntoView(item);
            SyncOutputs();
        }
    }

    private void RandomizeCategory_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetCategory(sender, out var category) ||
            ViewModel.GetMode(category) != PromptSelectionMode.Random)
        {
            return;
        }

        ViewModel.RandomizeCategory(category);
        SyncControls();
    }

    private void RandomizeAll_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.RandomizeAll();
        SyncControls();
    }

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
            await ViewModel.SaveHistoryAsync(
                PositiveOutput.Text ?? string.Empty,
                NegativeOutput.Text ?? string.Empty);
            StatusText.Text = "최근 기록에 저장했습니다.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"최근 기록 저장 실패: {ex.Message}";
        }
    }

    private void SyncControls()
    {
        if (!_initialized)
        {
            return;
        }

        _syncing = true;
        try
        {
            UpdateCategoryControls(
                PromptCategory.Character,
                CharacterDirectRadio,
                CharacterRandomRadio,
                CharacterDisabledRadio,
                CharacterSelectedList,
                CharacterSelectionSummary,
                CharacterEmptySelectionText,
                CharacterDirectControls,
                CharacterRandomControls,
                CharacterRandomCountCombo);

            UpdateCategoryControls(
                PromptCategory.Artist,
                ArtistDirectRadio,
                ArtistRandomRadio,
                ArtistDisabledRadio,
                ArtistSelectedList,
                ArtistSelectionSummary,
                ArtistEmptySelectionText,
                ArtistDirectControls,
                ArtistRandomControls,
                ArtistRandomCountCombo);

            UpdateCategoryControls(
                PromptCategory.Additional,
                AdditionalDirectRadio,
                AdditionalRandomRadio,
                AdditionalDisabledRadio,
                AdditionalSelectedList,
                AdditionalSelectionSummary,
                AdditionalEmptySelectionText,
                AdditionalDirectControls,
                AdditionalRandomControls,
                AdditionalRandomCountCombo);

            SyncOutputs();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateCategoryControls(
        PromptCategory category,
        RadioButton directRadio,
        RadioButton randomRadio,
        RadioButton disabledRadio,
        ListBox selectedList,
        TextBlock summary,
        TextBlock emptyText,
        Panel directControls,
        Panel randomControls,
        ComboBox randomCountCombo)
    {
        var mode = ViewModel.GetMode(category);
        var selectedCount = ViewModel.GetSelectedItems(category).Count;

        directRadio.IsChecked = mode == PromptSelectionMode.Fixed;
        randomRadio.IsChecked = mode == PromptSelectionMode.Random;
        disabledRadio.IsChecked = mode == PromptSelectionMode.Disabled;

        summary.Text = mode switch
        {
            PromptSelectionMode.Fixed => $"직접 선택 {selectedCount}개",
            PromptSelectionMode.Random => $"랜덤 결과 {selectedCount}개",
            _ => "미사용"
        };

        emptyText.Visibility = selectedCount == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        selectedList.Opacity = mode == PromptSelectionMode.Disabled ? 0.55 : 1.0;
        directControls.IsEnabled = mode == PromptSelectionMode.Fixed;
        randomControls.IsEnabled = mode == PromptSelectionMode.Random;

        var currentCount = ViewModel.GetRandomCount(category);
        var max = Math.Max(
            1,
            Math.Max(ViewModel.GetAvailableItems(category).Count, currentCount));

        randomCountCombo.ItemsSource = Enumerable.Range(1, max).ToArray();
        randomCountCombo.SelectedItem = currentCount;
    }

    private void SyncOutputs()
    {
        PositiveOutput.Text = ViewModel.PositiveText;
        NegativeOutput.Text = ViewModel.NegativeText;
    }

    private ListBox GetSelectedList(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterSelectedList,
        PromptCategory.Artist => ArtistSelectedList,
        PromptCategory.Additional => AdditionalSelectedList,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static bool TryGetCategory(object sender, out PromptCategory category)
    {
        category = default;

        return sender is FrameworkElement element &&
               element.Tag is string tag &&
               Enum.TryParse(tag, out category);
    }
}
