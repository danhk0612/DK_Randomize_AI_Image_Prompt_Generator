using System.IO.Compression;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class SettingsAndBackupTests
{
    [Fact]
    public async Task ThemeSettingPersistsAcrossServiceInstances()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var paths = AppDataPaths.Create(root);
            var settings = new SettingsService(paths);

            await settings.SetThemeAsync(AppTheme.Dark);

            var reloaded = new SettingsService(paths);
            await reloaded.LoadAsync();

            Assert.Equal(AppTheme.Dark, reloaded.Current.Theme);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public async Task WindowPlacementSettingsPersistAcrossServiceInstances()
    {
        var root = CreateTemporaryRoot();
        try
        {
            var paths = AppDataPaths.Create(root);
            var settings = new SettingsService(paths);
            settings.Current.WindowLeft = 120;
            settings.Current.WindowTop = 80;
            settings.Current.WindowWidth = 1440;
            settings.Current.WindowHeight = 900;
            settings.Current.WindowMaximized = true;
            await settings.SaveAsync();

            var reloaded = new SettingsService(paths);
            await reloaded.LoadAsync();

            Assert.Equal(120, reloaded.Current.WindowLeft);
            Assert.Equal(80, reloaded.Current.WindowTop);
            Assert.Equal(1440, reloaded.Current.WindowWidth);
            Assert.Equal(900, reloaded.Current.WindowHeight);
            Assert.True(reloaded.Current.WindowMaximized);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void PortableModeMarkerCanBeEnabledAndDisabled()
    {
        var root = CreateTemporaryRoot();
        try
        {
            Directory.CreateDirectory(root);

            Assert.False(AppDataPaths.IsPortableModeEnabled(root));

            AppDataPaths.SetPortableModeEnabled(true, root);
            Assert.True(AppDataPaths.IsPortableModeEnabled(root));
            Assert.True(File.Exists(AppDataPaths.GetPortableModeMarkerPath(root)));

            AppDataPaths.SetPortableModeEnabled(false, root);
            Assert.False(AppDataPaths.IsPortableModeEnabled(root));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public void GitHubUpdateVersionComparisonHandlesStableAndPrereleaseVersions()
    {
        Assert.True(GitHubUpdateService.IsNewerVersion("1.0.0-rc.2", "1.0.0-rc.1"));
        Assert.True(GitHubUpdateService.IsNewerVersion("v1.0.0", "1.0.0-rc.2"));
        Assert.True(GitHubUpdateService.IsNewerVersion("1.1.0", "1.0.9"));
        Assert.False(GitHubUpdateService.IsNewerVersion("1.0.0-rc.1", "1.0.0-rc.1"));
        Assert.False(GitHubUpdateService.IsNewerVersion("1.0.0-beta.2", "1.0.0-rc.1"));
    }

    [Fact]
    public async Task BackupAndRestoreRoundTripsDatabaseImagesAndSettings()
    {
        var root = CreateTemporaryRoot();
        var backupPath = Path.Combine(Path.GetTempPath(), $"dk-prompt-backup-{Guid.NewGuid():N}.zip");

        try
        {
            var paths = AppDataPaths.Create(root);
            var database = new DatabaseService(paths);
            await database.InitializeAsync();

            var prompts = new PromptRepository(database);
            var historyRepository = new HistoryRepository(database);

            var prompt = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Backup Character",
                PositivePrompt = "character positive",
                NegativePrompt = "character negative",
                ImagePath = Path.Combine("images", "sample.png")
            };
            prompt.Tags.Add("backup");

            var secondCharacter = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Backup Character Two",
                PositivePrompt = "character two positive"
            };

            var additional = new PromptItem
            {
                Category = PromptCategory.Additional,
                Title = "Backup Additional",
                PositivePrompt = "additional positive"
            };

            await prompts.CreateAsync(prompt);
            await prompts.CreateAsync(secondCharacter);
            await prompts.CreateAsync(additional);

            var history = new CombinationHistory
            {
                CharacterMode = PromptSelectionMode.Fixed,
                ArtistMode = PromptSelectionMode.Disabled,
                AdditionalMode = PromptSelectionMode.Random,
                AdditionalRandomCount = 2,
                PositiveText = "edited backup positive",
                NegativeText = "edited backup negative"
            };
            history.Items.AddRange(
            [
                new CombinationHistoryItem(PromptCategory.Character, secondCharacter.Id, secondCharacter.Title, 0),
                new CombinationHistoryItem(PromptCategory.Character, prompt.Id, prompt.Title, 1),
                new CombinationHistoryItem(PromptCategory.Additional, additional.Id, additional.Title, 0)
            ]);
            await historyRepository.SaveAsync(history);

            Directory.CreateDirectory(paths.ImagesDirectory);
            var imagePath = Path.Combine(paths.ImagesDirectory, "sample.png");
            await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3, 4 });

            var settings = new SettingsService(paths);
            await settings.SetThemeAsync(AppTheme.Dark);

            var backup = new BackupService(paths);
            await backup.CreateAsync(backupPath);

            await prompts.DeleteAsync(prompt.Id);
            await prompts.DeleteAsync(secondCharacter.Id);
            await prompts.DeleteAsync(additional.Id);
            await historyRepository.DeleteAsync(history.Id);
            File.Delete(imagePath);
            await settings.SetThemeAsync(AppTheme.Light);

            await backup.RestoreAsync(backupPath);
            await database.InitializeAsync();

            var restoredPrompt = await prompts.GetByIdAsync(prompt.Id);
            var restoredSettings = new SettingsService(paths);
            await restoredSettings.LoadAsync();

            var restoredHistory = await historyRepository.GetByIdAsync(history.Id);

            Assert.NotNull(restoredPrompt);
            Assert.Equal("Backup Character", restoredPrompt.Title);
            Assert.Contains("backup", restoredPrompt.Tags);
            Assert.True(File.Exists(imagePath));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(imagePath));
            Assert.Equal(AppTheme.Dark, restoredSettings.Current.Theme);

            Assert.NotNull(restoredHistory);
            Assert.Equal(
                new Guid?[] { secondCharacter.Id, prompt.Id },
                restoredHistory.GetItems(PromptCategory.Character)
                    .Select(item => item.PromptId)
                    .ToArray());
            Assert.Equal(additional.Id, restoredHistory.GetItems(PromptCategory.Additional).Single().PromptId);
            Assert.Equal(PromptSelectionMode.Fixed, restoredHistory.CharacterMode);
            Assert.Equal(PromptSelectionMode.Disabled, restoredHistory.ArtistMode);
            Assert.Equal(PromptSelectionMode.Random, restoredHistory.AdditionalMode);
            Assert.Equal(2, restoredHistory.AdditionalRandomCount);
            Assert.Equal("edited backup positive", restoredHistory.PositiveText);
            Assert.Equal("edited backup negative", restoredHistory.NegativeText);
        }
        finally
        {
            DeleteTemporaryRoot(root);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    [Fact]
    public async Task CorruptedSettingsFileFallsBackToDefaultsAndIsPreserved()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var paths = AppDataPaths.Create(root);
            paths.EnsureDirectories();
            await File.WriteAllTextAsync(paths.SettingsPath, "{ this is not valid json");

            var settings = new SettingsService(paths);
            await settings.LoadAsync();

            Assert.Equal(AppTheme.System, settings.Current.Theme);
            Assert.True(File.Exists(paths.SettingsPath));

            var preserved = Directory
                .EnumerateFiles(root, "settings.corrupt-*.json")
                .ToArray();
            Assert.Single(preserved);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    [Fact]
    public async Task InvalidBackupDoesNotDeleteExistingImages()
    {
        var root = CreateTemporaryRoot();
        var backupPath = Path.Combine(Path.GetTempPath(), $"dk-prompt-invalid-backup-{Guid.NewGuid():N}.zip");

        try
        {
            var paths = AppDataPaths.Create(root);
            paths.EnsureDirectories();

            var existingImagePath = Path.Combine(paths.ImagesDirectory, "keep.png");
            await File.WriteAllBytesAsync(existingImagePath, new byte[] { 9, 8, 7 });

            using (var stream = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                var imageEntry = archive.CreateEntry("images/replacement.png");
                await using var entryStream = imageEntry.Open();
                await entryStream.WriteAsync(new byte[] { 1, 2, 3 });
            }

            var backup = new BackupService(paths);

            await Assert.ThrowsAsync<InvalidDataException>(() => backup.RestoreAsync(backupPath));

            Assert.True(File.Exists(existingImagePath));
            Assert.Equal(new byte[] { 9, 8, 7 }, await File.ReadAllBytesAsync(existingImagePath));
        }
        finally
        {
            DeleteTemporaryRoot(root);
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    private static string CreateTemporaryRoot() =>
        Path.Combine(Path.GetTempPath(), $"dk-prompt-tests-{Guid.NewGuid():N}");

    private static void DeleteTemporaryRoot(string root)
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
