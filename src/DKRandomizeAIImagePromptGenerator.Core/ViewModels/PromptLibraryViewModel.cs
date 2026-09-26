using System.Collections.ObjectModel;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.ViewModels;

public sealed class PromptLibraryViewModel
{
    public const int PageSize = 60;

    private readonly PromptRepository _repository;

    public PromptLibraryViewModel(PromptRepository repository)
    {
        _repository = repository;
    }

    public ObservableCollection<PromptItem> Items { get; } = [];

    public PromptCategory SelectedCategory { get; private set; } = PromptCategory.Character;

    public string SearchText { get; set; } = string.Empty;

    public string TagFilter { get; set; } = string.Empty;

    public PromptLibrarySortOrder SortOrder { get; set; } = PromptLibrarySortOrder.UpdatedNewest;

    public int CurrentPageIndex { get; private set; }

    public int TotalCount { get; private set; }

    public int TotalPages =>
        TotalCount == 0
            ? 0
            : (TotalCount + PageSize - 1) / PageSize;

    public bool HasPreviousPage => CurrentPageIndex > 0;

    public bool HasNextPage => CurrentPageIndex + 1 < TotalPages;

    public async Task SetCategoryAsync(PromptCategory category)
    {
        if (SelectedCategory == category)
        {
            return;
        }

        SelectedCategory = category;
        ResetToFirstPage();
        await RefreshAsync();
    }

    public void ResetToFirstPage() => CurrentPageIndex = 0;

    public async Task GoToPageAsync(int pageIndex)
    {
        if (pageIndex < 0)
        {
            pageIndex = 0;
        }

        if (TotalPages > 0)
        {
            pageIndex = Math.Min(pageIndex, TotalPages - 1);
        }

        CurrentPageIndex = pageIndex;
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        var page = await LoadCurrentPageAsync();
        TotalCount = page.TotalCount;

        var lastPageIndex =
            TotalCount == 0
                ? 0
                : (TotalCount - 1) / PageSize;

        if (CurrentPageIndex > lastPageIndex)
        {
            CurrentPageIndex = lastPageIndex;
            page = await LoadCurrentPageAsync();
            TotalCount = page.TotalCount;
        }

        Items.Clear();
        foreach (var item in page.Items)
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

    private Task<PromptSearchPage> LoadCurrentPageAsync() =>
        _repository.SearchPageAsync(
            SelectedCategory,
            SearchText,
            TagFilter,
            SortOrder,
            CurrentPageIndex,
            PageSize);

    private static IEnumerable<string> NormalizeTags(IEnumerable<string> tags) =>
        tags.Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
}
