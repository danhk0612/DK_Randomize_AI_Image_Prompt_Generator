using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class CombinationServiceTests
{
    private readonly CombinationService _service = new();

    [Fact]
    public void FixedSelectionsUseRequestedItemsInCategoryOrder()
    {
        var character = Item(PromptCategory.Character, "Character", "character +", "character -");
        var artist = Item(PromptCategory.Artist, "Artist", "artist +", "artist -");
        var additional = Item(PromptCategory.Additional, "Additional", "additional +", "additional -");
        var items = new[] { additional, artist, character };

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Fixed, character.Id),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Fixed, artist.Id),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Fixed, additional.Id));

        var result = _service.Combine(items, request);
        var separator = Environment.NewLine + Environment.NewLine;

        Assert.Same(character, result.Character);
        Assert.Same(artist, result.Artist);
        Assert.Single(result.AdditionalItems);
        Assert.Same(additional, result.AdditionalItems[0]);
        Assert.Equal($"character +{separator}artist +{separator}additional +", result.PositiveText);
        Assert.Equal($"character -{separator}artist -{separator}additional -", result.NegativeText);
    }

    [Fact]
    public void DisabledSelectionContributesNoItemOrText()
    {
        var character = Item(PromptCategory.Character, "Character", "character +", "character -");
        var artist = Item(PromptCategory.Artist, "Artist", "artist +", "artist -");

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Fixed, character.Id),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Disabled),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Disabled));

        var result = _service.Combine(new[] { character, artist }, request);

        Assert.Same(character, result.Character);
        Assert.Null(result.Artist);
        Assert.Empty(result.AdditionalItems);
        Assert.Equal("character +", result.PositiveText);
        Assert.Equal("character -", result.NegativeText);
    }

    [Fact]
    public void RandomSelectionOnlyUsesRequestedCategory()
    {
        var characterA = Item(PromptCategory.Character, "Character A", "a", string.Empty);
        var characterB = Item(PromptCategory.Character, "Character B", "b", string.Empty);
        var artist = Item(PromptCategory.Artist, "Artist", "artist", string.Empty);

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Random),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Disabled),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Disabled));

        var result = _service.Combine(
            new[] { characterA, artist, characterB },
            request,
            new Random(1234));

        Assert.NotNull(result.Character);
        Assert.Equal(PromptCategory.Character, result.Character.Category);
        Assert.Contains(result.Character, new[] { characterA, characterB });
        Assert.Null(result.Artist);
    }

    [Fact]
    public void EmptyPromptSectionsAreSkippedWithoutExtraBlankLines()
    {
        var character = Item(PromptCategory.Character, "Character", " character + ", "   ");
        var artist = Item(PromptCategory.Artist, "Artist", "   ", " artist - ");
        var additional = Item(PromptCategory.Additional, "Additional", " additional + ", string.Empty);

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Fixed, character.Id),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Fixed, artist.Id),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Fixed, additional.Id));

        var result = _service.Combine(new[] { character, artist, additional }, request);
        var separator = Environment.NewLine + Environment.NewLine;

        Assert.Equal($"character +{separator}additional +", result.PositiveText);
        Assert.Equal("artist -", result.NegativeText);
    }

    [Fact]
    public void RandomSelectionWithNoCandidatesProducesNormalEmptyResult()
    {
        var request = PromptCombinationRequest.Default;

        var result = _service.Combine(Array.Empty<PromptItem>(), request, new Random(1));

        Assert.Null(result.Character);
        Assert.Null(result.Artist);
        Assert.Empty(result.AdditionalItems);
        Assert.Equal(string.Empty, result.PositiveText);
        Assert.Equal(string.Empty, result.NegativeText);
    }

    private static PromptItem Item(
        PromptCategory category,
        string title,
        string positive,
        string negative)
    {
        return new PromptItem
        {
            Category = category,
            Title = title,
            PositivePrompt = positive,
            NegativePrompt = negative
        };
    }
}
