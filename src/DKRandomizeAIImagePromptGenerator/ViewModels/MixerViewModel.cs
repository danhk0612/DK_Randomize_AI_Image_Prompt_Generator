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

    public PromptSelectionMode CharacterMode { get; private set; } = PromptSelectionMode.Random;

    public PromptSelectionMode ArtistMode { get; private set; } = PromptSelectionMode.Random;

    public PromptSelectionMode AdditionalMode { get; private set; } = PromptSelectionMode.Random;

    public PromptItem? SelectedCharacter { get; private set; }

    public PromptItem? SelectedArtist { get; private set; }

    public PromptItem? SelectedAdditional { get; private set; }

    public string PositiveText { get; private set; } = string.Empty;

    public string NegativeText { get; private set; } = string.Empty;

    public async Task LoadAsync()
    {
        await LoadCategoryAsync(CharacterItems, PromptCategory.Character);
        await LoadCategoryAsync(ArtistItems, PromptCategory.Artist);
        await LoadCategoryAsync(AdditionalItems, PromptCategory.Additional);
        RandomizeAll();
    }

    public void SetMode(PromptCategory category, PromptSelectionMode mode)
    {
        switch (category)
        {
            case PromptCategory.Character:
                CharacterMode = mode;
                if (mode == PromptSelectionMode.Disabled)
                {
                    SelectedCharacter = null;
                }
                else if (mode == PromptSelectionMode.Fixed && SelectedCharacter is null)
                {
                    SelectedCharacter = CharacterItems.FirstOrDefault();
                }
                break;
            case PromptCategory.Artist:
                ArtistMode = mode;
                if (mode == PromptSelectionMode.Disabled)
                {
                    SelectedArtist = null;
                }
                else if (mode == PromptSelectionMode.Fixed && SelectedArtist is null)
                {
                    SelectedArtist = ArtistItems.FirstOrDefault();
                }
                break;
            case PromptCategory.Additional:
                AdditionalMode = mode;
                if (mode == PromptSelectionMode.Disabled)
                {
                    SelectedAdditional = null;
                }
                else if (mode == PromptSelectionMode.Fixed && SelectedAdditional is null)
                {
                    SelectedAdditional = AdditionalItems.FirstOrDefault();
                }
                break;
        }

        if (mode == PromptSelectionMode.Random)
        {
            RandomizeCategory(category);
        }
        else
        {
            RecomposeCurrent();
        }
    }

    public void SetSelectedItem(PromptCategory category, PromptItem? item)
    {
        switch (category)
        {
            case PromptCategory.Character:
                SelectedCharacter = item;
                break;
            case PromptCategory.Artist:
                SelectedArtist = item;
                break;
            case PromptCategory.Additional:
                SelectedAdditional = item;
                break;
        }

        RecomposeCurrent();
    }

    public void RandomizeAll()
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                BuildSelection(PromptCategory.Character, CharacterMode, SelectedCharacter, allowRandom: true),
                BuildSelection(PromptCategory.Artist, ArtistMode, SelectedArtist, allowRandom: true),
                BuildSelection(PromptCategory.Additional, AdditionalMode, SelectedAdditional, allowRandom: true)));

        Apply(result);
    }

    public void RandomizeCategory(PromptCategory category)
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                BuildSelection(
                    PromptCategory.Character,
                    CharacterMode,
                    SelectedCharacter,
                    allowRandom: category == PromptCategory.Character),
                BuildSelection(
                    PromptCategory.Artist,
                    ArtistMode,
                    SelectedArtist,
                    allowRandom: category == PromptCategory.Artist),
                BuildSelection(
                    PromptCategory.Additional,
                    AdditionalMode,
                    SelectedAdditional,
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
            PositiveText = positiveText,
            NegativeText = negativeText
        };

        if (SelectedAdditional is not null)
        {
            record.AdditionalItems.Add(
                new CombinationHistoryAdditional(
                    SelectedAdditional.Id,
                    SelectedAdditional.Title));
        }

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

    private void RecomposeCurrent()
    {
        var result = _combinationService.Combine(
            GetAllItems(),
            new PromptCombinationRequest(
                StableSelection(PromptCategory.Character, CharacterMode, SelectedCharacter),
                StableSelection(PromptCategory.Artist, ArtistMode, SelectedArtist),
                StableSelection(PromptCategory.Additional, AdditionalMode, SelectedAdditional)));

        Apply(result);
    }

    private IReadOnlyCollection<PromptItem> GetAllItems() =>
        CharacterItems.Concat(ArtistItems).Concat(AdditionalItems).ToArray();

    private static PromptSelection BuildSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        PromptItem? selected,
        bool allowRandom)
    {
        if (mode == PromptSelectionMode.Disabled)
        {
            return new PromptSelection(category, PromptSelectionMode.Disabled);
        }

        if (mode == PromptSelectionMode.Random && allowRandom)
        {
            return new PromptSelection(category, PromptSelectionMode.Random);
        }

        return selected is null
            ? new PromptSelection(category, PromptSelectionMode.Disabled)
            : new PromptSelection(category, PromptSelectionMode.Fixed, selected.Id);
    }

    private static PromptSelection StableSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        PromptItem? selected)
    {
        if (mode == PromptSelectionMode.Disabled || selected is null)
        {
            return new PromptSelection(category, PromptSelectionMode.Disabled);
        }

        return new PromptSelection(category, PromptSelectionMode.Fixed, selected.Id);
    }

    private void Apply(PromptCombination combination)
    {
        SelectedCharacter = combination.Character;
        SelectedArtist = combination.Artist;
        SelectedAdditional = combination.AdditionalItems.FirstOrDefault();
        PositiveText = combination.PositiveText;
        NegativeText = combination.NegativeText;
    }
}
