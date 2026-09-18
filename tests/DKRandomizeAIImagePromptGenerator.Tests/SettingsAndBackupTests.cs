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
            var prompt = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Backup Character",
                PositivePrompt = "character positive",
                NegativePrompt = "character negative",
                ImagePath = Path.Combine("images", "sample.png")
            };
            prompt.Tags.Add("backup");
            await prompts.CreateAsync(prompt);

            Directory.CreateDirectory(paths.ImagesDirectory);
            var imagePath = Path.Combine(paths.ImagesDirectory, "sample.png");
            await File.WriteAllBytesAsync(imagePath, new byte[] { 1, 2, 3, 4 });

            var settings = new SettingsService(paths);
            await settings.SetThemeAsync(AppTheme.Dark);

            var backup = new BackupService(paths);
            await backup.CreateAsync(backupPath);

            await prompts.DeleteAsync(prompt.Id);
            File.Delete(imagePath);
            await settings.SetThemeAsync(AppTheme.Light);

            await backup.RestoreAsync(backupPath);
            await database.InitializeAsync();

            var restoredPrompt = await prompts.GetByIdAsync(prompt.Id);
            var restoredSettings = new SettingsService(paths);
            await restoredSettings.LoadAsync();

            Assert.NotNull(restoredPrompt);
            Assert.Equal("Backup Character", restoredPrompt.Title);
            Assert.Contains("backup", restoredPrompt.Tags);
            Assert.True(File.Exists(imagePath));
            Assert.Equal(new byte[] { 1, 2, 3, 4 }, await File.ReadAllBytesAsync(imagePath));
            Assert.Equal(AppTheme.Dark, restoredSettings.Current.Theme);
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
