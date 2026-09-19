using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class CombinationService
{
    private static readonly string SectionSeparator = Environment.NewLine;

    public PromptCombination Combine(
        IReadOnlyCollection<PromptItem> items,
        PromptCombinationRequest request,
        Random? random = null)
    {
        random ??= Random.Shared;

        var characters = Resolve(items, request.Character, random);
        var artists = Resolve(items, request.Artist, random);
        var additionalItems = Resolve(items, request.Additional, random);

        var orderedItems = characters
            .Concat(artists)
            .Concat(additionalItems)
            .ToArray();

        return new PromptCombination(
            characters,
            artists,
            additionalItems,
            Compose(orderedItems, item => item.PositivePrompt),
            Compose(orderedItems, item => item.NegativePrompt));
    }

    private static IReadOnlyList<PromptItem> Resolve(
        IReadOnlyCollection<PromptItem> items,
        PromptSelection selection,
        Random random)
    {
        if (selection.Mode == PromptSelectionMode.Disabled)
        {
            return Array.Empty<PromptItem>();
        }

        var candidates = items
            .Where(item => item.Category == selection.Category)
            .ToArray();

        if (selection.Mode == PromptSelectionMode.Fixed)
        {
            if (selection.FixedPromptIds.Count == 0)
            {
                return Array.Empty<PromptItem>();
            }

            var byId = candidates.ToDictionary(item => item.Id);
            var selected = new List<PromptItem>(selection.FixedPromptIds.Count);

            foreach (var id in selection.FixedPromptIds)
            {
                if (byId.TryGetValue(id, out var item))
                {
                    selected.Add(item);
                }
            }

            return selected;
        }

        if (candidates.Length == 0)
        {
            return Array.Empty<PromptItem>();
        }

        var count = Math.Min(selection.RandomCount, candidates.Length);
        var pool = candidates.ToArray();

        for (var index = 0; index < count; index++)
        {
            var selectedIndex = random.Next(index, pool.Length);
            (pool[index], pool[selectedIndex]) = (pool[selectedIndex], pool[index]);
        }

        return pool.Take(count).ToArray();
    }

    private static string Compose(
        IEnumerable<PromptItem> items,
        Func<PromptItem, string> selector)
    {
        return string.Join(
            SectionSeparator,
            items.Select(selector)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => text.Trim()));
    }
}
