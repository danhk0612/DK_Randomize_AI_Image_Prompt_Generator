using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class CombinationService
{
    private static readonly string SectionSeparator = Environment.NewLine + Environment.NewLine;

    public PromptCombination Combine(
        IReadOnlyCollection<PromptItem> items,
        PromptCombinationRequest request,
        Random? random = null)
    {
        random ??= Random.Shared;

        var character = Resolve(items, request.Character, random);
        var artist = Resolve(items, request.Artist, random);
        var additional = Resolve(items, request.Additional, random);
        IReadOnlyList<PromptItem> additionalItems = additional is null
            ? Array.Empty<PromptItem>()
            : new[] { additional };

        var orderedItems = new List<PromptItem>(2 + additionalItems.Count);

        if (character is not null)
        {
            orderedItems.Add(character);
        }

        if (artist is not null)
        {
            orderedItems.Add(artist);
        }

        orderedItems.AddRange(additionalItems);

        return new PromptCombination(
            character,
            artist,
            additionalItems,
            Compose(orderedItems, item => item.PositivePrompt),
            Compose(orderedItems, item => item.NegativePrompt));
    }

    private static PromptItem? Resolve(
        IReadOnlyCollection<PromptItem> items,
        PromptSelection selection,
        Random random)
    {
        if (selection.Mode == PromptSelectionMode.Disabled)
        {
            return null;
        }

        var candidates = items
            .Where(item => item.Category == selection.Category)
            .ToArray();

        if (selection.Mode == PromptSelectionMode.Fixed)
        {
            if (selection.FixedPromptId is null)
            {
                return null;
            }

            return candidates.FirstOrDefault(item => item.Id == selection.FixedPromptId.Value);
        }

        if (candidates.Length == 0)
        {
            return null;
        }

        return candidates[random.Next(candidates.Length)];
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
