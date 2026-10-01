namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed record PromptSelection
{
    public PromptSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        Guid? fixedPromptId = null,
        int randomCount = 1,
        IReadOnlyList<string>? requiredTags = null)
        : this(
            category,
            mode,
            fixedPromptId is Guid id ? new[] { id } : Array.Empty<Guid>(),
            randomCount,
            requiredTags)
    {
    }

    public PromptSelection(
        PromptCategory category,
        PromptSelectionMode mode,
        IReadOnlyList<Guid> fixedPromptIds,
        int randomCount = 1,
        IReadOnlyList<string>? requiredTags = null)
    {
        Category = category;
        Mode = mode;
        FixedPromptIds = fixedPromptIds
            .Distinct()
            .ToArray();
        RandomCount = Math.Max(1, randomCount);
        RequiredTags = (requiredTags ?? Array.Empty<string>())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public PromptCategory Category { get; }

    public PromptSelectionMode Mode { get; }

    public IReadOnlyList<Guid> FixedPromptIds { get; }

    public Guid? FixedPromptId =>
        FixedPromptIds.Count == 0 ? null : FixedPromptIds[0];

    public int RandomCount { get; }

    public IReadOnlyList<string> RequiredTags { get; }
}
