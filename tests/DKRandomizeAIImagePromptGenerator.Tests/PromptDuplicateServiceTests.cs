using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class PromptDuplicateServiceTests
{
    private readonly PromptDuplicateService _service = new();

    [Fact]
    public void FindsExactDuplicateIgnoringTitleAndFormattingWhitespace()
    {
        var existing = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "기존",
            PositivePrompt = "1girl, long hair, blue eyes",
            NegativePrompt = "low quality, blurry"
        };

        var match = _service.FindBestMatch(
            "  1girl,long   hair, blue eyes  ",
            "low quality,blurry",
            [existing]);

        Assert.NotNull(match);
        Assert.True(match.IsExact);
        Assert.Equal(1.0, match.Similarity);
        Assert.Equal(existing.Id, match.Item.Id);
    }

    [Fact]
    public void FindsVerySimilarPromptAtNinetyFivePercent()
    {
        var common = Enumerable.Range(1, 19)
            .Select(index => $"tag{index}")
            .ToArray();

        var existing = new PromptItem
        {
            Category = PromptCategory.Additional,
            Title = "기존",
            PositivePrompt = string.Join(", ", common.Append("original"))
        };

        var match = _service.FindBestMatch(
            string.Join(", ", common.Append("changed")),
            string.Empty,
            [existing]);

        Assert.NotNull(match);
        Assert.False(match.IsExact);
        Assert.Equal(0.95, match.Similarity, precision: 10);
    }

    [Fact]
    public void DoesNotReportPromptBelowSimilarityThreshold()
    {
        var existing = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "기존",
            PositivePrompt = "1girl, long hair, blue eyes, smile"
        };

        var match = _service.FindBestMatch(
            "1girl, short hair, red eyes, serious",
            string.Empty,
            [existing]);

        Assert.Null(match);
    }

    [Fact]
    public void EmptyPromptBodiesAreNotTreatedAsDuplicates()
    {
        var existing = new PromptItem
        {
            Category = PromptCategory.Character,
            Title = "제목만 있는 항목"
        };

        var match = _service.FindBestMatch(
            string.Empty,
            string.Empty,
            [existing]);

        Assert.Null(match);
        Assert.Empty(_service.FindPairs([existing]));
    }

    [Fact]
    public void PairScanReturnsExactAndVerySimilarPairs()
    {
        var common = Enumerable.Range(1, 19)
            .Select(index => $"tag{index}")
            .ToArray();

        var first = new PromptItem
        {
            Category = PromptCategory.Additional,
            Title = "A",
            PositivePrompt = string.Join(", ", common.Append("original"))
        };
        var exact = new PromptItem
        {
            Category = PromptCategory.Additional,
            Title = "B",
            PositivePrompt = first.PositivePrompt
        };
        var similar = new PromptItem
        {
            Category = PromptCategory.Additional,
            Title = "C",
            PositivePrompt = string.Join(", ", common.Append("changed"))
        };

        var pairs = _service.FindPairs([first, exact, similar]);

        Assert.Contains(
            pairs,
            pair => pair.IsExact &&
                    ((pair.First.Id == first.Id && pair.Second.Id == exact.Id) ||
                     (pair.First.Id == exact.Id && pair.Second.Id == first.Id)));
        Assert.Contains(
            pairs,
            pair => !pair.IsExact && Math.Abs(pair.Similarity - 0.95) < 0.000001);
    }
}
