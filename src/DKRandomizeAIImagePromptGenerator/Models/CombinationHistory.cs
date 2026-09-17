namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed class CombinationHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid? CharacterPromptId { get; init; }

    public string? CharacterTitleSnapshot { get; init; }

    public Guid? ArtistPromptId { get; init; }

    public string? ArtistTitleSnapshot { get; init; }

    public List<CombinationHistoryAdditional> AdditionalItems { get; init; } = [];

    public required string PositiveText { get; init; }

    public required string NegativeText { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CombinationHistoryAdditional(
    Guid? PromptId,
    string? TitleSnapshot);
