using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task PromptRepositoryCreateReadAndTagSearchPreserveData()
    {
        var (root, database) = await CreateDatabaseAsync();

        try
        {
            var repository = new PromptRepository(database);
            var prompt = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Elf Swordswoman",
                PositivePrompt = "silver hair, elf ears",
                NegativePrompt = "bad anatomy",
                Memo = "SDXL reference",
                ImagePath = "images/character.webp"
            };
            prompt.Tags.AddRange(["Fantasy", "Sword"]);

            await repository.CreateAsync(prompt);

            var loaded = await repository.GetByIdAsync(prompt.Id);
            var tagResults = await repository.SearchAsync(
                PromptCategory.Character,
                searchText: "silver hair",
                tag: "fantasy");

            Assert.NotNull(loaded);
            Assert.Equal(prompt.Title, loaded.Title);
            Assert.Equal(prompt.PositivePrompt, loaded.PositivePrompt);
            Assert.Equal(prompt.NegativePrompt, loaded.NegativePrompt);
            Assert.Equal(prompt.Memo, loaded.Memo);
            Assert.Equal(prompt.ImagePath, loaded.ImagePath);
            Assert.Equal(new[] { "Fantasy", "Sword" }, loaded.Tags);
            Assert.Single(tagResults);
            Assert.Equal(prompt.Id, tagResults[0].Id);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task PromptRepositoryUpdateReplacesTagsAndDeleteRemovesPrompt()
    {
        var (root, database) = await CreateDatabaseAsync();

        try
        {
            var repository = new PromptRepository(database);
            var prompt = new PromptItem
            {
                Category = PromptCategory.Artist,
                Title = "Style A",
                PositivePrompt = "style prompt"
            };
            prompt.Tags.AddRange(["Anime", "Old"]);
            await repository.CreateAsync(prompt);

            prompt.Title = "Style B";
            prompt.Tags.Clear();
            prompt.Tags.AddRange(["Anime", "Clean"]);
            await repository.UpdateAsync(prompt);

            var loaded = await repository.GetByIdAsync(prompt.Id);
            Assert.NotNull(loaded);
            Assert.Equal("Style B", loaded.Title);
            Assert.Equal(new[] { "Anime", "Clean" }, loaded.Tags);

            await repository.DeleteAsync(prompt.Id);
            Assert.Null(await repository.GetByIdAsync(prompt.Id));
            Assert.Empty(await repository.SearchAsync(tag: "old"));
            Assert.Empty(await repository.SearchAsync(tag: "clean"));
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task HistoryRepositoryPreservesFinalEditedTextAndSnapshots()
    {
        var (root, database) = await CreateDatabaseAsync();

        try
        {
            var promptRepository = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var character = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Character A"
            };
            var artist = new PromptItem
            {
                Category = PromptCategory.Artist,
                Title = "Artist A"
            };
            var additional = new PromptItem
            {
                Category = PromptCategory.Additional,
                Title = "Rainy Street"
            };

            await promptRepository.CreateAsync(character);
            await promptRepository.CreateAsync(artist);
            await promptRepository.CreateAsync(additional);

            var history = new CombinationHistory
            {
                CharacterPromptId = character.Id,
                CharacterTitleSnapshot = character.Title,
                ArtistPromptId = artist.Id,
                ArtistTitleSnapshot = artist.Title,
                PositiveText = "manually edited positive result",
                NegativeText = "manually edited negative result"
            };
            history.AdditionalItems.Add(
                new CombinationHistoryAdditional(additional.Id, additional.Title));

            await historyRepository.SaveAsync(history);

            var loaded = await historyRepository.GetByIdAsync(history.Id);

            Assert.NotNull(loaded);
            Assert.Equal(history.PositiveText, loaded.PositiveText);
            Assert.Equal(history.NegativeText, loaded.NegativeText);
            Assert.Equal(character.Id, loaded.CharacterPromptId);
            Assert.Equal(character.Title, loaded.CharacterTitleSnapshot);
            Assert.Equal(artist.Id, loaded.ArtistPromptId);
            Assert.Equal(artist.Title, loaded.ArtistTitleSnapshot);
            Assert.Single(loaded.AdditionalItems);
            Assert.Equal(additional.Id, loaded.AdditionalItems[0].PromptId);
            Assert.Equal(additional.Title, loaded.AdditionalItems[0].TitleSnapshot);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task DeletingSourcePromptKeepsHistoryTextAndSnapshotReadable()
    {
        var (root, database) = await CreateDatabaseAsync();

        try
        {
            var promptRepository = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var character = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Disposable Character"
            };
            await promptRepository.CreateAsync(character);

            var history = new CombinationHistory
            {
                CharacterPromptId = character.Id,
                CharacterTitleSnapshot = character.Title,
                PositiveText = "kept positive",
                NegativeText = "kept negative"
            };
            await historyRepository.SaveAsync(history);

            await promptRepository.DeleteAsync(character.Id);
            var loaded = await historyRepository.GetByIdAsync(history.Id);

            Assert.NotNull(loaded);
            Assert.Null(loaded.CharacterPromptId);
            Assert.Equal("Disposable Character", loaded.CharacterTitleSnapshot);
            Assert.Equal("kept positive", loaded.PositiveText);
            Assert.Equal("kept negative", loaded.NegativeText);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static async Task<(string Root, DatabaseService Database)> CreateDatabaseAsync()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        var database = new DatabaseService(AppDataPaths.Create(root));
        await database.InitializeAsync();
        return (root, database);
    }

    private static void Cleanup(string root)
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
