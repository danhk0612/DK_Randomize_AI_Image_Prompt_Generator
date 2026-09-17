namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptSelection(
    PromptCategory Category,
    PromptSelectionMode Mode,
    Guid? FixedPromptId = null);
