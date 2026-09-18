namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptCombination(
    IReadOnlyList<PromptItem> CharacterItems,
    IReadOnlyList<PromptItem> ArtistItems,
    IReadOnlyList<PromptItem> AdditionalItems,
    string PositiveText,
    string NegativeText)
{
    public PromptItem? Character => CharacterItems.FirstOrDefault();

    public PromptItem? Artist => ArtistItems.FirstOrDefault();
}
