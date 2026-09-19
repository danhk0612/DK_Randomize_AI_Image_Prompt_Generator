using System.Collections.ObjectModel;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;

namespace DKRandomizeAIImagePromptGenerator.ViewModels;

public sealed class MixerViewModel
{
    private readonly PromptRepository _prompts;
    private readonly HistoryRepository _history;
    private readonly CombinationService _combinationService;

    public MixerViewModel(
        PromptRepository prompts,
        HistoryRepository history,
        CombinationService combinationService)
    {
        _prompts = prompts;
        _history = history;
        _combinationService = combinationService;
    }

    public ObservableCollection<PromptItem> CharacterItems { get; } = [];

    public ObservableCollection<PromptItem> ArtistItems { get; } = [];

    public ObservableCollection<PromptItem> AdditionalItems { get; } = [];

    public ObservableCollection<PromptItem> SelectedCharacters { get; } = [];

    public ObservableCollection<PromptItem> SelectedArtists { get; } = [];

    public ObservableCollection<PromptItem> SelectedAdditionals { get; } = [];

    public PromptSelectionMode CharacterMode { get; private set; } = PromptSelectionMode.Random;

    public PromptSelectionMode ArtistMode { get; private set; } = PromptSelectionMode.Random;

    public PromptSelectionMode AdditionalMode { get; private set; } = PromptSelectionMode.Random;

    public int CharacterRandomCount { get; private set; } = 1;

    public int ArtistRandomCount { get; private set; } = 1;

    public int AdditionalRandomCount { get; private set; } = 1;

    // Compatibility accessors while the WPF UI is migrated to multi-select.
    public PromptItem? SelectedCharacter => SelectedCharacters.FirstOrDefault();

    public PromptItem? SelectedArtist => SelectedArtists.FirstOrDefault();

    public PromptItem? SelectedAdditional => SelectedAdditionals.FirstOrDefault();

    public string PositiveText { get; private set; } = string.Empty;

    public string NegativeText { get; private set; } = string.Empty;

    public async Task LoadAsync()
    {
        await LoadCategoryAsync(CharacterItems, PromptCategory.Character);
        await LoadCategoryAsync(ArtistItems, PromptCategory.Artist);
        await LoadCategoryAsync(AdditionalItems, PromptCategory.Additional);
        RandomizeAll();
    }

    public IReadOnlyList<PromptItem> GetAvailableItems(PromptCategory category) =>
        GetAvailableCollection(category).ToArray();

    public IReadOnlyList<PromptItem> GetSelectedItems(PromptCategory category) =>
        GetSelectedCollection(category).ToArray();

    public PromptSelectionMode GetMode(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterMode,
        PromptCategory.Artist => ArtistMode,
        PromptCategory.Additional => AdditionalMode,
        _ => PromptSelectionMode.Disabled
    };

    public int GetRandomCount(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterRandomCount,
        PromptCategory.Artist => ArtistRandomCount,
        PromptCategory.Additional => AdditionalRandomCount,
        _ => 1
    };

    public void RestoreFromHistory(CombinationHistory history)
    {
        RestoreCategoryFromHistory(history, PromptCategory.Character);
        RestoreCategoryFromHistory(history, PromptCategory.Artist);
        RestoreCategoryFromHistory(history, PromptCategory.Additional);

        CharacterMode = history.CharacterMode;
        ArtistMode = history.ArtistMode;
        AdditionalMode = history.AdditionalMode;
        CharacterRandomCount = Math.Max(1, history.CharacterRandomCount);
        ArtistRandomCount = Math.Max(1, history.ArtistRandomCount);
        AdditionalRandomCount = Math.Max(1, history.AdditionalRandomCount);

        PositiveText = history.PositiveText;
        NegativeText = history.NegativeText;
    }

    public void SetMode(PromptCategory category, PromptSelectionMode mode)
    {
        SetModeValue(category, mode);

        if (mode == PromptSelectionMode.Disabled)
        {
            GetSelectedCollection(category).Clear();
            RecomposeCurrent();
            return;
        }

        if (mode == PromptSelectionMode.Random)
        {
            RandomizeCategory(category);
            return;
        }

        RecomposeCurrent();
    }

    public void SetRandomCount(PromptCategory category, int count)
    {
        count = Math.Max(1, count);

        switch (category)
        {
            case PromptCategory.Character:
                CharacterRandomCount = count;
                break;
            case PromptCategory.Artist:
                ArtistRandomCount = count;
                break;
            case PromptCategory.Additional:
                AdditionalRandomCount = count;
                break;
        }

        if (GetMode(category) == PromptSelectionMode.Random)
        {
            RandomizeCategory(category);
        }
    }

    public void SetSelectedItems(
        PromptCategory category,
        IEnumerable<PromptItem> items,
        bool switchToDirectMode = true)
    {
        var selected = GetSelectedCollection(category);
        var normalized = items
            .Where(item => item.Category == category)
            .GroupBy(item => item.Id)
            .Select(group => group.First())
            .ToArray();

        selected.Clear();
        foreach (var item in normalized)
        {
            selected.Add(item);
        }

        if (switchToDirectMode)
        {
            SetModeValue(category, PromptSelectionMode.Fixed);
        }

        RecomposeCurrent();
    }

    public bool AddSelectedItem(
        PromptCategory category,
        PromptItem item,
        bool switchToDirectMode = true)
    {
        if (item.Category != category)
        {
            return false;
        }

        UpsertAvailableItem(item);

        var selected = GetSelectedCollection(category);
        if (selected.Any(existing => existing.Id == item.Id))
        {
            return false;
        }

        selected.Add(item);

        if (switchToDirectMode)
        {
            SetModeValue(category, PromptSelectionMode.Fixed);
        }

        RecomposeCurrent();
        return true;
    }

    public void UpsertAvailableItem(PromptItem item)
    {
        var available = GetAvailableCollection(item.Category);
        var availableIndex = IndexOfPrompt(available, item.Id);

        if (availableIndex >= 0)
        {
            available[availableIndex] = item;
        }
        else
        {
            available.Add(item);
        }

        var selected = GetSelectedCollection(item.Category);
        var selectedIndex = IndexOfPrompt(selected, item.Id);
        if (selectedIndex >= 0)
        {
            selected[selectedIndex] = item;
        }
    }

    public bool RemoveAvailableItem(PromptCategory category, Guid promptId)
    {
        var available = GetAvailableCollection(category);
        var availableIndex = IndexOfPrompt(available, promptId);
        if (availableIndex >= 0)
        {
            available.RemoveAt(availableIndex);
        }

        var selected = GetSelectedCollection(category);
        var selectedIndex = IndexOfPrompt(selected, promptId);
        if (selectedIndex < 0)
        {
            return availableIndex >= 0;
        }

        selected.RemoveAt(selectedIndex);
        RecomposeCurrent();
        return true;
    }

    public bool RemoveSelectedItem(PromptCategory category, Guid promptId)
    {
        var selected = GetSelectedCollection(category);
        var item = selected.FirstOrDefault(candidate => candidate.Id == promptId);
        if (item is null)
        {
            return false;
        }

        selected.Remove(item);
        RecomposeCurrent();
        return true;
    }

    public bool MoveSelectedItem(PromptCategory category, int fromIndex, int toIndex)
    {
        var selected = GetSelectedCollection(category);

        if (fromIndex < 0 ||
            fromIndex >= selected.Count ||
            toIndex < 0 ||
            toIndex >= selected.Count ||
            fromIndex == toIndex)
        {
            return false;
        }

        selected.Move(fromIndex, toIndex);
        RecomposeCurrent();
        return true;
    }

    // Compatibility wrapper for the previous single-select UI.
    public void SetSelectedItem(PromptCategory category, PromptItem? item)
    {
        SetSelectedItems(
            category,
            item is null ? Array.Empty<PromptItem>() : new[] { item });
    }

    public void RandomizeAll()
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                BuildSelection(PromptCategory.Character, allowRandom: true),
                BuildSelection(PromptCategory.Artist, allowRandom: true),
                BuildSelection(PromptCategory.Additional, allowRandom: true)));

        Apply(result);
    }

    public void RandomizeCategory(PromptCategory category)
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                BuildSelection(
                    PromptCategory.Character,
                    allowRandom: category == PromptCategory.Character),
                BuildSelection(
                    PromptCategory.Artist,
                    allowRandom: category == PromptCategory.Artist),
                BuildSelection(
                    PromptCategory.Additional,
                    allowRandom: category == PromptCategory.Additional)));

        Apply(result);
    }

    public async Task SaveHistoryAsync(string positiveText, string negativeText)
    {
        var record = new CombinationHistory
        {
            CharacterPromptId = SelectedCharacter?.Id,
            CharacterTitleSnapshot = SelectedCharacter?.Title,
            ArtistPromptId = SelectedArtist?.Id,
            ArtistTitleSnapshot = SelectedArtist?.Title,
            CharacterMode = CharacterMode,
            ArtistMode = ArtistMode,
            AdditionalMode = AdditionalMode,
            CharacterRandomCount = CharacterRandomCount,
            ArtistRandomCount = ArtistRandomCount,
            AdditionalRandomCount = AdditionalRandomCount,
            PositiveText = positiveText,
            NegativeText = negativeText
        };

        AddHistoryItems(record, PromptCategory.Character, SelectedCharacters);
        AddHistoryItems(record, PromptCategory.Artist, SelectedArtists);
        AddHistoryItems(record, PromptCategory.Additional, SelectedAdditionals);

        record.AdditionalItems.AddRange(
            SelectedAdditionals.Select(item =>
                new CombinationHistoryAdditional(item.Id, item.Title)));

        await _history.SaveAsync(record);
    }

    private async Task LoadCategoryAsync(
        ObservableCollection<PromptItem> target,
        PromptCategory category)
    {
        var items = await _prompts.SearchAsync(category);
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private void RestoreCategoryFromHistory(
        CombinationHistory history,
        PromptCategory category)
    {
        var available = GetAvailableCollection(category);
        var selected = GetSelectedCollection(category);
        selected.Clear();

        var historyItems = history.GetItems(category);
        if (historyItems.Count > 0)
        {
            foreach (var historyItem in historyItems)
            {
                if (historyItem.PromptId is not Guid id)
                {
                    continue;
                }

                var item = available.FirstOrDefault(candidate => candidate.Id == id);
                if (item is not null)
                {
                    selected.Add(item);
                }
            }

            return;
        }

        // Compatibility for in-memory V1 history objects not loaded through schema v2.
        IEnumerable<Guid?> legacyIds = category switch
        {
            PromptCategory.Character => new[] { history.CharacterPromptId },
            PromptCategory.Artist => new[] { history.ArtistPromptId },
            PromptCategory.Additional => history.AdditionalItems.Select(item => item.PromptId),
            _ => Array.Empty<Guid?>()
        };

        foreach (var id in legacyIds.OfType<Guid>())
        {
            var item = available.FirstOrDefault(candidate => candidate.Id == id);
            if (item is not null)
            {
                selected.Add(item);
            }
        }
    }

    private void RecomposeCurrent()
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                StableSelection(PromptCategory.Character),
                StableSelection(PromptCategory.Artist),
                StableSelection(PromptCategory.Additional)));

        Apply(result);
    }

    private IReadOnlyCollection<PromptItem> GetAllItems() =>
        CharacterItems.Concat(ArtistItems).Concat(AdditionalItems).ToArray();

    private PromptSelection BuildSelection(
        PromptCategory category,
        bool allowRandom)
    {
        var mode = GetMode(category);
        var selected = GetSelectedCollection(category);

        if (mode == PromptSelectionMode.Disabled)
        {
            return new PromptSelection(category, PromptSelectionMode.Disabled);
        }

        if (mode == PromptSelectionMode.Random && allowRandom)
        {
            return new PromptSelection(
                category,
                PromptSelectionMode.Random,
                fixedPromptId: null,
                randomCount: GetRandomCount(category));
        }

        return new PromptSelection(
            category,
            PromptSelectionMode.Fixed,
            selected.Select(item => item.Id).ToArray());
    }

    private PromptSelection StableSelection(PromptCategory category)
    {
        if (GetMode(category) == PromptSelectionMode.Disabled)
        {
            return new PromptSelection(category, PromptSelectionMode.Disabled);
        }

        return new PromptSelection(
            category,
            PromptSelectionMode.Fixed,
            GetSelectedCollection(category).Select(item => item.Id).ToArray());
    }

    private void Apply(PromptCombination combination)
    {
        ReplaceSelected(SelectedCharacters, combination.CharacterItems);
        ReplaceSelected(SelectedArtists, combination.ArtistItems);
        ReplaceSelected(SelectedAdditionals, combination.AdditionalItems);
        PositiveText = combination.PositiveText;
        NegativeText = combination.NegativeText;
    }

    private static void ReplaceSelected(
        ObservableCollection<PromptItem> target,
        IReadOnlyList<PromptItem> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    private static void AddHistoryItems(
        CombinationHistory history,
        PromptCategory category,
        IEnumerable<PromptItem> items)
    {
        history.Items.AddRange(
            items.Select((item, index) =>
                new CombinationHistoryItem(
                    category,
                    item.Id,
                    item.Title,
                    index)));
    }

    private ObservableCollection<PromptItem> GetAvailableCollection(
        PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterItems,
        PromptCategory.Artist => ArtistItems,
        PromptCategory.Additional => AdditionalItems,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private ObservableCollection<PromptItem> GetSelectedCollection(
        PromptCategory category) => category switch
    {
        PromptCategory.Character => SelectedCharacters,
        PromptCategory.Artist => SelectedArtists,
        PromptCategory.Additional => SelectedAdditionals,
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    private static int IndexOfPrompt(
        ObservableCollection<PromptItem> items,
        Guid promptId)
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index].Id == promptId)
            {
                return index;
            }
        }

        return -1;
    }

    private void SetModeValue(PromptCategory category, PromptSelectionMode mode)
    {
        switch (category)
        {
            case PromptCategory.Character:
                CharacterMode = mode;
                break;
            case PromptCategory.Artist:
                ArtistMode = mode;
                break;
            case PromptCategory.Additional:
                AdditionalMode = mode;
                break;
        }
    }
}
