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

    public ObservableCollection<CombinationHistory> Items { get; } = [];

    public async Task RefreshAsync()
    {
        var items = await _repository.GetRecentAsync();
        Items.Clear();

        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    public async Task DeleteAsync(CombinationHistory history)
    {
        await _repository.DeleteAsync(history.Id);
        await RefreshAsync();
    }
}
