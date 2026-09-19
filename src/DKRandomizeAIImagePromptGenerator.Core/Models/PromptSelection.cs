namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptSelection
{
    public PromptSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        Guid? fixedPromptId = null,
        int randomCount = 1)
        : this(
            category,
            mode,
            fixedPromptId is Guid id ? new[] { id } : Array.Empty<Guid>(),
            randomCount)
    {
    }

    public PromptSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        IReadOnlyList<Guid> fixedPromptIds,
        int randomCount = 1)
    {
        Category = category;
        Mode = mode;
        FixedPromptIds = fixedPromptIds
            .Distinct()
            .ToArray();
        RandomCount = Math.Max(1, randomCount);
    }

    public PromptCategory Category { get; }

    public PromptSelectionMode Mode { get; }

    public IReadOnlyList<Guid> FixedPromptIds { get; }

    public Guid? FixedPromptId =>
        FixedPromptIds.Count == 0 ? null : FixedPromptIds[0];

    public int RandomCount { get; }
}
