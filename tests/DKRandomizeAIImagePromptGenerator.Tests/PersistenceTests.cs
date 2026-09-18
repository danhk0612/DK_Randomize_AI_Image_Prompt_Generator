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

    [Fact]
    public async Task HistoryRepositoryPreservesMultipleItemsOrderModesAndRandomCounts()
    {
        var (root, database) = await CreateDatabaseAsync();

        try
        {
            var promptRepository = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var characterA = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Character A"
            };
            var characterB = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Character B"
            };
            var artist = new PromptItem
            {
                Category = PromptCategory.Artist,
                Title = "Artist A"
            };
            var additionalA = new PromptItem
            {
                Category = PromptCategory.Additional,
                Title = "Additional A"
            };
            var additionalB = new PromptItem
            {
                Category = PromptCategory.Additional,
                Title = "Additional B"
            };

            foreach (var prompt in new[] { characterA, characterB, artist, additionalA, additionalB })
            {
                await promptRepository.CreateAsync(prompt);
            }

            var history = new CombinationHistory
            {
                CharacterMode = PromptSelectionMode.Fixed,
                ArtistMode = PromptSelectionMode.Random,
                AdditionalMode = PromptSelectionMode.Fixed,
                CharacterRandomCount = 1,
                ArtistRandomCount = 2,
                AdditionalRandomCount = 3,
                PositiveText = "multi positive",
                NegativeText = "multi negative"
            };

            history.Items.AddRange(
            [
                new CombinationHistoryItem(PromptCategory.Character, characterB.Id, characterB.Title, 0),
                new CombinationHistoryItem(PromptCategory.Character, characterA.Id, characterA.Title, 1),
                new CombinationHistoryItem(PromptCategory.Artist, artist.Id, artist.Title, 0),
                new CombinationHistoryItem(PromptCategory.Additional, additionalB.Id, additionalB.Title, 0),
                new CombinationHistoryItem(PromptCategory.Additional, additionalA.Id, additionalA.Title, 1)
            ]);

            await historyRepository.SaveAsync(history);

            var loaded = await historyRepository.GetByIdAsync(history.Id);

            Assert.NotNull(loaded);
            Assert.Equal(
                new Guid?[] { characterB.Id, characterA.Id },
                loaded.GetItems(PromptCategory.Character).Select(item => item.PromptId).ToArray());
            Assert.Equal(
                new Guid?[] { additionalB.Id, additionalA.Id },
                loaded.GetItems(PromptCategory.Additional).Select(item => item.PromptId).ToArray());

            Assert.Equal(PromptSelectionMode.Fixed, loaded.CharacterMode);
            Assert.Equal(PromptSelectionMode.Random, loaded.ArtistMode);
            Assert.Equal(PromptSelectionMode.Fixed, loaded.AdditionalMode);
            Assert.Equal(1, loaded.CharacterRandomCount);
            Assert.Equal(2, loaded.ArtistRandomCount);
            Assert.Equal(3, loaded.AdditionalRandomCount);

            // Legacy compatibility still exposes the first item and Additional list.
            Assert.Equal(characterB.Id, loaded.CharacterPromptId);
            Assert.Equal(artist.Id, loaded.ArtistPromptId);
            Assert.Equal(
                new Guid?[] { additionalB.Id, additionalA.Id },
                loaded.AdditionalItems.Select(item => item.PromptId).ToArray());
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task Version1HistoryMigratesToVersion2WithoutLosingSelections()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        var paths = AppDataPaths.Create(root);
        paths.EnsureDirectories();

        var characterId = Guid.NewGuid();
        var artistId = Guid.NewGuid();
        var additionalId = Guid.NewGuid();
        var historyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.ToString("O");

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = paths.DatabasePath
            }.ToString();

            await using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = """
                    PRAGMA foreign_keys = ON;

                    CREATE TABLE PromptItems (
                        Id TEXT PRIMARY KEY NOT NULL,
                        Category INTEGER NOT NULL,
                        Title TEXT NOT NULL,
                        PositivePrompt TEXT NULL,
                        NegativePrompt TEXT NULL,
                        Memo TEXT NULL,
                        ImagePath TEXT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        UpdatedAtUtc TEXT NOT NULL
                    );

                    CREATE TABLE CombinationHistory (
                        Id TEXT PRIMARY KEY NOT NULL,
                        CharacterPromptId TEXT NULL,
                        CharacterTitleSnapshot TEXT NULL,
                        ArtistPromptId TEXT NULL,
                        ArtistTitleSnapshot TEXT NULL,
                        PositiveText TEXT NOT NULL,
                        NegativeText TEXT NOT NULL,
                        CreatedAtUtc TEXT NOT NULL,
                        FOREIGN KEY (CharacterPromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL,
                        FOREIGN KEY (ArtistPromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL
                    );

                    CREATE TABLE CombinationHistoryAdditional (
                        HistoryId TEXT NOT NULL,
                        PromptId TEXT NULL,
                        SortOrder INTEGER NOT NULL,
                        TitleSnapshot TEXT NULL,
                        PRIMARY KEY (HistoryId, SortOrder),
                        FOREIGN KEY (HistoryId) REFERENCES CombinationHistory(Id) ON DELETE CASCADE,
                        FOREIGN KEY (PromptId) REFERENCES PromptItems(Id) ON DELETE SET NULL
                    );

                    INSERT INTO PromptItems (
                        Id, Category, Title, PositivePrompt, NegativePrompt,
                        Memo, ImagePath, CreatedAtUtc, UpdatedAtUtc)
                    VALUES
                        (@characterId, 0, 'Legacy Character', '', '', '', NULL, @now, @now),
                        (@artistId, 1, 'Legacy Artist', '', '', '', NULL, @now, @now),
                        (@additionalId, 2, 'Legacy Additional', '', '', '', NULL, @now, @now);

                    INSERT INTO CombinationHistory (
                        Id, CharacterPromptId, CharacterTitleSnapshot,
                        ArtistPromptId, ArtistTitleSnapshot,
                        PositiveText, NegativeText, CreatedAtUtc)
                    VALUES (
                        @historyId, @characterId, 'Legacy Character',
                        @artistId, 'Legacy Artist',
                        'legacy positive', 'legacy negative', @now);

                    INSERT INTO CombinationHistoryAdditional (
                        HistoryId, PromptId, SortOrder, TitleSnapshot)
                    VALUES (
                        @historyId, @additionalId, 0, 'Legacy Additional');

                    PRAGMA user_version = 1;
                    """;
                command.Parameters.AddWithValue("@characterId", characterId.ToString("D"));
                command.Parameters.AddWithValue("@artistId", artistId.ToString("D"));
                command.Parameters.AddWithValue("@additionalId", additionalId.ToString("D"));
                command.Parameters.AddWithValue("@historyId", historyId.ToString("D"));
                command.Parameters.AddWithValue("@now", now);
                await command.ExecuteNonQueryAsync();
            }

            var database = new DatabaseService(paths);
            await database.InitializeAsync();

            await using (var connection = await database.OpenConnectionAsync())
            {
                await using var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = "PRAGMA user_version;";
                Assert.Equal(2L, (long)(await versionCommand.ExecuteScalarAsync() ?? 0L));
            }

            var historyRepository = new HistoryRepository(database);
            var loaded = await historyRepository.GetByIdAsync(historyId);

            Assert.NotNull(loaded);
            Assert.Equal(characterId, loaded.GetItems(PromptCategory.Character).Single().PromptId);
            Assert.Equal(artistId, loaded.GetItems(PromptCategory.Artist).Single().PromptId);
            Assert.Equal(additionalId, loaded.GetItems(PromptCategory.Additional).Single().PromptId);
            Assert.Equal(PromptSelectionMode.Random, loaded.CharacterMode);
            Assert.Equal(PromptSelectionMode.Random, loaded.ArtistMode);
            Assert.Equal(PromptSelectionMode.Random, loaded.AdditionalMode);
            Assert.Equal(1, loaded.CharacterRandomCount);
            Assert.Equal(1, loaded.ArtistRandomCount);
            Assert.Equal(1, loaded.AdditionalRandomCount);
            Assert.Equal("legacy positive", loaded.PositiveText);
            Assert.Equal("legacy negative", loaded.NegativeText);
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
