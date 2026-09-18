using System.Collections.ObjectModel;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.ViewModels;

public sealed class HistoryViewModel
{
    private readonly HistoryRepository _repository;

    public HistoryViewModel(HistoryRepository repository)
    {
        _repository = repository;
    }

    public const int PageSize = 20;

    public ObservableCollection<CombinationHistory> Items { get; } = [];

    public int CurrentPage { get; private set; } = 1;

    public int TotalPages { get; private set; } = 1;

    public int TotalCount { get; private set; }

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;

    public async Task RefreshAsync(int? requestedPage = null)
    {
        TotalCount = await _repository.GetCountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));

        CurrentPage = Math.Clamp(
            requestedPage ?? CurrentPage,
            1,
            TotalPages);

        var items = await _repository.GetPageAsync(
            CurrentPage - 1,
            PageSize);

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public Task PreviousPageAsync() =>
        HasPreviousPage
            ? RefreshAsync(CurrentPage - 1)
            : Task.CompletedTask;

    public Task NextPageAsync() =>
        HasNextPage
            ? RefreshAsync(CurrentPage + 1)
            : Task.CompletedTask;

    public async Task DeleteAsync(CombinationHistory history)
    {
        await _repository.DeleteAsync(history.Id);
        await RefreshAsync(CurrentPage);
    }
}
