using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class CombinationServiceTests
{
    private readonly CombinationService _service = new();

    [Fact]
    public void FixedSelectionsUseRequestedItemsInCategoryAndSelectionOrder()
    {
        var characterA = Item(PromptCategory.Character, "Character A", "character a +", "character a -");
        var characterB = Item(PromptCategory.Character, "Character B", "character b +", "character b -");
        var artist = Item(PromptCategory.Artist, "Artist", "artist +", "artist -");
        var additional = Item(PromptCategory.Additional, "Additional", "additional +", "additional -");
        var items = new[] { additional, characterB, artist, characterA };

        var request = new PromptCombinationRequest(
            new PromptSelection(
                PromptCategory.Character,
                PromptSelectionMode.Fixed,
                new[] { characterB.Id, characterA.Id }),
            new PromptSelection(
                PromptCategory.Artist,
                PromptSelectionMode.Fixed,
                new[] { artist.Id }),
            new PromptSelection(
                PromptCategory.Additional,
                PromptSelectionMode.Fixed,
                new[] { additional.Id }));

        var result = _service.Combine(items, request);
        var separator = Environment.NewLine;

        Assert.Equal(new[] { characterB, characterA }, result.CharacterItems);
        Assert.Single(result.ArtistItems);
        Assert.Same(artist, result.ArtistItems[0]);
        Assert.Single(result.AdditionalItems);
        Assert.Same(additional, result.AdditionalItems[0]);
        Assert.Equal(
            string.Join(separator, "character b +", "character a +", "artist +", "additional +"),
            result.PositiveText);
        Assert.Equal(
            string.Join(separator, "character b -", "character a -", "artist -", "additional -"),
            result.NegativeText);
    }

    [Fact]
    public void LegacySingleFixedSelectionRemainsSupported()
    {
        var character = Item(PromptCategory.Character, "Character", "character +", "character -");

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Fixed, character.Id),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Disabled),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Disabled));

        var result = _service.Combine(new[] { character }, request);

        Assert.Same(character, result.Character);
        Assert.Single(result.CharacterItems);
        Assert.Equal("character +", result.PositiveText);
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

        Assert.Single(result.CharacterItems);
        Assert.Empty(result.ArtistItems);
        Assert.Empty(result.AdditionalItems);
        Assert.Equal("character +", result.PositiveText);
        Assert.Equal("character -", result.NegativeText);
    }

    [Fact]
    public void RandomSelectionUsesRequestedCountWithoutDuplicates()
    {
        var characters = Enumerable.Range(1, 5)
            .Select(index => Item(
                PromptCategory.Character,
                $"Character {index}",
                $"character {index}",
                string.Empty))
            .ToArray();
        var artist = Item(PromptCategory.Artist, "Artist", "artist", string.Empty);

        var request = new PromptCombinationRequest(
            new PromptSelection(
                PromptCategory.Character,
                PromptSelectionMode.Random,
                fixedPromptId: null,
                randomCount: 3),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Disabled),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Disabled));

        var result = _service.Combine(
            characters.Append(artist).ToArray(),
            request,
            new Random(1234));

        Assert.Equal(3, result.CharacterItems.Count);
        Assert.Equal(3, result.CharacterItems.Select(item => item.Id).Distinct().Count());
        Assert.All(result.CharacterItems, item => Assert.Equal(PromptCategory.Character, item.Category));
        Assert.DoesNotContain(artist, result.CharacterItems);
    }

    [Fact]
    public void RandomCountIsCappedAtAvailableCandidates()
    {
        var a = Item(PromptCategory.Additional, "A", "a", string.Empty);
        var b = Item(PromptCategory.Additional, "B", "b", string.Empty);

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Disabled),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Disabled),
            new PromptSelection(
                PromptCategory.Additional,
                PromptSelectionMode.Random,
                fixedPromptId: null,
                randomCount: 99));

        var result = _service.Combine(new[] { a, b }, request, new Random(7));

        Assert.Equal(2, result.AdditionalItems.Count);
        Assert.Equal(2, result.AdditionalItems.Select(item => item.Id).Distinct().Count());
    }

    [Fact]
    public void EmptyPromptSectionsAreSkippedWithoutExtraLines()
    {
        var character = Item(PromptCategory.Character, "Character", " character + ", "   ");
        var artist = Item(PromptCategory.Artist, "Artist", "   ", " artist - ");
        var additional = Item(PromptCategory.Additional, "Additional", " additional + ", string.Empty);

        var request = new PromptCombinationRequest(
            new PromptSelection(PromptCategory.Character, PromptSelectionMode.Fixed, character.Id),
            new PromptSelection(PromptCategory.Artist, PromptSelectionMode.Fixed, artist.Id),
            new PromptSelection(PromptCategory.Additional, PromptSelectionMode.Fixed, additional.Id));

        var result = _service.Combine(new[] { character, artist, additional }, request);

        Assert.Equal($"character +{Environment.NewLine}additional +", result.PositiveText);
        Assert.Equal("artist -", result.NegativeText);
    }

    [Fact]
    public void RandomSelectionWithNoCandidatesProducesNormalEmptyResult()
    {
        var request = PromptCombinationRequest.Default;

        var result = _service.Combine(Array.Empty<PromptItem>(), request, new Random(1));

        Assert.Empty(result.CharacterItems);
        Assert.Empty(result.ArtistItems);
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
