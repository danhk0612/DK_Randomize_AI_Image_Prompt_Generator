namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed class PromptItem
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required PromptCategory Category { get; init; }

    public required string Title { get; set; }

    public string PositivePrompt { get; set; } = string.Empty;

    public string NegativePrompt { get; set; } = string.Empty;

    public string Memo { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public List<string> Tags { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Session-only Mixer state. This is intentionally not persisted by PromptRepository.
    public bool IsMixerEnabled { get; set; } = true;

    // Session-only UI state used to disable direct-selection controls in Random mode.
    public bool IsMixerDirectControlEnabled { get; set; } = true;
}
