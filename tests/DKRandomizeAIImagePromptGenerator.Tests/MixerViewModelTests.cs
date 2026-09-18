using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using DKRandomizeAIImagePromptGenerator.ViewModels;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class MixerViewModelTests
{
    [Fact]
    public async Task DirectMultiSelectPreservesOrderAndUsesSingleNewlines()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        var database = new DatabaseService(AppDataPaths.Create(root));
        await database.InitializeAsync();

        try
        {
            var prompts = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var first = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "First",
                PositivePrompt = "first"
            };
            var second = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Second",
                PositivePrompt = "second"
            };

            await prompts.CreateAsync(first);
            await prompts.CreateAsync(second);

            var viewModel = new MixerViewModel(
                prompts,
                historyRepository,
                new CombinationService());
            await viewModel.LoadAsync();

            viewModel.SetSelectedItems(
                PromptCategory.Character,
                new[] { second, first });
            viewModel.SetMode(PromptCategory.Artist, PromptSelectionMode.Disabled);
            viewModel.SetMode(PromptCategory.Additional, PromptSelectionMode.Disabled);

            Assert.Equal(
                new[] { second.Id, first.Id },
                viewModel.SelectedCharacters.Select(item => item.Id).ToArray());
            Assert.Equal(
                $"second{Environment.NewLine}first",
                viewModel.PositiveText);

            Assert.True(viewModel.MoveSelectedItem(PromptCategory.Character, 1, 0));
            Assert.Equal(
                $"first{Environment.NewLine}second",
                viewModel.PositiveText);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RandomCountSelectsMultipleUniquePrompts()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        var database = new DatabaseService(AppDataPaths.Create(root));
        await database.InitializeAsync();

        try
        {
            var prompts = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            for (var index = 1; index <= 5; index++)
            {
                await prompts.CreateAsync(new PromptItem
                {
                    Category = PromptCategory.Additional,
                    Title = $"Additional {index}",
                    PositivePrompt = $"additional {index}"
                });
            }

            var viewModel = new MixerViewModel(
                prompts,
                historyRepository,
                new CombinationService());
            await viewModel.LoadAsync();

            viewModel.SetMode(PromptCategory.Character, PromptSelectionMode.Disabled);
            viewModel.SetMode(PromptCategory.Artist, PromptSelectionMode.Disabled);
            viewModel.SetRandomCount(PromptCategory.Additional, 3);
            viewModel.SetMode(PromptCategory.Additional, PromptSelectionMode.Random);

            Assert.Equal(3, viewModel.SelectedAdditionals.Count);
            Assert.Equal(
                3,
                viewModel.SelectedAdditionals.Select(item => item.Id).Distinct().Count());
            Assert.Equal(3, viewModel.PositiveText.Split(Environment.NewLine).Length);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RestoreFromHistoryKeepsSelectionsButAllowsImmediateReroll()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        var database = new DatabaseService(AppDataPaths.Create(root));
        await database.InitializeAsync();

        try
        {
            var prompts = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var character = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Character A",
                PositivePrompt = "character prompt"
            };
            var artist = new PromptItem
            {
                Category = PromptCategory.Artist,
                Title = "Artist A",
                PositivePrompt = "artist prompt"
            };
            var additional = new PromptItem
            {
                Category = PromptCategory.Additional,
                Title = "Additional A",
                PositivePrompt = "additional prompt"
            };

            await prompts.CreateAsync(character);
            await prompts.CreateAsync(artist);
            await prompts.CreateAsync(additional);

            var viewModel = new MixerViewModel(
                prompts,
                historyRepository,
                new CombinationService());
            await viewModel.LoadAsync();

            var history = new CombinationHistory
            {
                CharacterPromptId = character.Id,
                CharacterTitleSnapshot = character.Title,
                ArtistPromptId = artist.Id,
                ArtistTitleSnapshot = artist.Title,
                PositiveText = "manually edited restored text",
                NegativeText = "restored negative"
            };
            history.AdditionalItems.Add(
                new CombinationHistoryAdditional(additional.Id, additional.Title));

            viewModel.RestoreFromHistory(history);

            Assert.Equal(character.Id, viewModel.SelectedCharacter?.Id);
            Assert.Equal(artist.Id, viewModel.SelectedArtist?.Id);
            Assert.Equal(additional.Id, viewModel.SelectedAdditional?.Id);
            Assert.Equal(PromptSelectionMode.Random, viewModel.CharacterMode);
            Assert.Equal(PromptSelectionMode.Random, viewModel.ArtistMode);
            Assert.Equal(PromptSelectionMode.Random, viewModel.AdditionalMode);
            Assert.Equal("manually edited restored text", viewModel.PositiveText);

            viewModel.RandomizeAll();

            Assert.Equal(
                string.Join(Environment.NewLine,
                    "character prompt",
                    "artist prompt",
                    "additional prompt"),
                viewModel.PositiveText);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
