using System.Collections.ObjectModel;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.ViewModels;

public sealed class PromptLibraryViewModel
{
    private readonly PromptRepository _repository;

    public PromptLibraryViewModel(PromptRepository repository)
    {
        _repository = repository;
    }

    public ObservableCollection<PromptItem> Items { get; } = [];

    public PromptCategory SelectedCategory { get; private set; } = PromptCategory.Character;

    public string SearchText { get; set; } = string.Empty;

    public string TagFilter { get; set; } = string.Empty;

    public async Task SetCategoryAsync(PromptCategory category)
    {
        if (SelectedCategory == category)
        {
            return;
        }

        SelectedCategory = category;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var results = await _repository.SearchAsync(
            SelectedCategory,
            SearchText,
            TagFilter);

        Items.Clear();
        foreach (var item in results)
        {
            Items.Add(item);
        }
    }

    public async Task<PromptItem> CreateAsync(
        string title,
        string positivePrompt,
        string negativePrompt,
        string memo,
        string? imagePath,
        IEnumerable<string> tags)
    {
        var item = new PromptItem
        {
            Category = SelectedCategory,
            Title = title.Trim(),
            PositivePrompt = positivePrompt,
            NegativePrompt = negativePrompt,
            Memo = memo,
            ImagePath = imagePath
        };
        item.Tags.AddRange(NormalizeTags(tags));

        await _repository.CreateAsync(item);
        await RefreshAsync();
        return item;
    }

    public async Task UpdateAsync(
        PromptItem item,
        string title,
        string positivePrompt,
        string negativePrompt,
        string memo,
        string? imagePath,
        IEnumerable<string> tags)
    {
        item.Title = title.Trim();
        item.PositivePrompt = positivePrompt;
        item.NegativePrompt = negativePrompt;
        item.Memo = memo;
        item.ImagePath = imagePath;
        item.Tags.Clear();
        item.Tags.AddRange(NormalizeTags(tags));

        await _repository.UpdateAsync(item);
        await RefreshAsync();
    }

    public async Task<PromptItem> DuplicateAsync(PromptItem source)
    {
        var copy = new PromptItem
        {
            Category = source.Category,
            Title = $"{source.Title} 복사본",
            PositivePrompt = source.PositivePrompt,
            NegativePrompt = source.NegativePrompt,
            Memo = source.Memo,
            ImagePath = source.ImagePath
        };
        copy.Tags.AddRange(source.Tags);

        await _repository.CreateAsync(copy);
        await RefreshAsync();
        return copy;
    }

    public async Task DeleteAsync(PromptItem item)
    {
        await _repository.DeleteAsync(item.Id);
        await RefreshAsync();
    }

    private static IEnumerable<string> NormalizeTags(IEnumerable<string> tags) =>
        tags.Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
