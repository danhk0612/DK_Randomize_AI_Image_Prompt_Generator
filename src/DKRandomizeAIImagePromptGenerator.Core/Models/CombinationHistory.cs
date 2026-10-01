namespace DKRandomizeAIImagePromptGenerator.Models;

public sealed class CombinationHistory
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // Legacy V1 compatibility fields. V2 persistence uses Items as the source of truth.
    public Guid? CharacterPromptId { get; set; }

    public string? CharacterTitleSnapshot { get; set; }

    public Guid? ArtistPromptId { get; set; }

    public string? ArtistTitleSnapshot { get; set; }

    public List<CombinationHistoryAdditional> AdditionalItems { get; init; } = [];

    public List<CombinationHistoryItem> Items { get; init; } = [];

    public PromptSelectionMode CharacterMode { get; set; } = PromptSelectionMode.Random;

    public PromptSelectionMode ArtistMode { get; set; } = PromptSelectionMode.Random;

    public PromptSelectionMode AdditionalMode { get; set; } = PromptSelectionMode.Random;

    public int CharacterRandomCount { get; set; } = 1;

    public int ArtistRandomCount { get; set; } = 1;

    public int AdditionalRandomCount { get; set; } = 1;

    public List<string> CharacterRandomTags { get; } = [];

    public List<string> ArtistRandomTags { get; } = [];

    public List<string> AdditionalRandomTags { get; } = [];

    public required string PositiveText { get; init; }

    public required string NegativeText { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public string CreatedAtLocalText =>
        CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string CharacterTitleSummary => BuildTitleSummary(PromptCategory.Character);

    public string ArtistTitleSummary => BuildTitleSummary(PromptCategory.Artist);

    public string AdditionalTitleSummary => BuildTitleSummary(PromptCategory.Additional);

    public IReadOnlyList<CombinationHistoryItem> GetItems(PromptCategory category) =>
        Items
            .Where(item => item.Category == category)
            .OrderBy(item => item.SortOrder)
            .ToArray();

    private string BuildTitleSummary(PromptCategory category)
    {
        var titles = GetItems(category)
            .Select(item => item.TitleSnapshot ?? "삭제된 프롬프트")
            .ToArray();

        return titles.Length == 0 ? "없음" : string.Join(", ", titles);
    }

    public PromptSelectionMode GetMode(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterMode,
        PromptCategory.Artist => ArtistMode,
        PromptCategory.Additional => AdditionalMode,
        _ => PromptSelectionMode.Disabled
    };

    public int GetRandomCount(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterRandomCount,
        PromptCategory.Artist => ArtistRandomCount,
        PromptCategory.Additional => AdditionalRandomCount,
        _ => 1
    };

    public IReadOnlyList<string> GetRandomTags(PromptCategory category) => category switch
    {
        PromptCategory.Character => CharacterRandomTags,
        PromptCategory.Artist => ArtistRandomTags,
        PromptCategory.Additional => AdditionalRandomTags,
        _ => Array.Empty<string>()
    };

    public void SetRandomTags(PromptCategory category, IEnumerable<string> tags)
    {
        var target = category switch
        {
            PromptCategory.Character => CharacterRandomTags,
            PromptCategory.Artist => ArtistRandomTags,
            PromptCategory.Additional => AdditionalRandomTags,
            _ => throw new ArgumentOutOfRangeException(nameof(category))
        };

        target.Clear();
        target.AddRange(
            tags.Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }
}

public sealed record CombinationHistoryItem(
    PromptCategory Category,
    Guid? PromptId,
    string? TitleSnapshot,
    int SortOrder,
    bool IsEnabled = true);

public sealed record CombinationHistoryAdditional(
    Guid? PromptId,
    string? TitleSnapshot);
