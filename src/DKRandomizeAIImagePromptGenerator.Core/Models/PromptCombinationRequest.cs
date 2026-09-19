namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptCombinationRequest(
    PromptSelection Character,
    PromptSelection Artist,
    PromptSelection Additional)
{
    public static PromptCombinationRequest Default { get; } = new(
        new PromptSelection(PromptCategory.Character, PromptSelectionMode.Random),
        new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Random),
        new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Random));
}
