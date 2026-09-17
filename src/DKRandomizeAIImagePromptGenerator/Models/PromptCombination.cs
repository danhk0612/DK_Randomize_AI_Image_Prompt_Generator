namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptCombination(
    PromptItem? Character,
    PromptItem? Artist,
    IReadOnlyList<PromptItem> AdditionalItems,
    string PositiveText,
    string NegativeText);
