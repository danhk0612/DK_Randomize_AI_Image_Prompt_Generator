using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class ImageStorageServiceTests
{
    [Fact]
    public async Task ImportedImageIsKeptWhileReferencedAndDeletedAfterLastReference()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(root);
            var sourcePath = Path.Combine(root, "source.png");
            await File.WriteAllBytesAsync(sourcePath, [1, 2, 3, 4]);

            var paths = AppDataPaths.Create(root);
            var database = new DatabaseService(paths);
            await database.InitializeAsync();
            var prompts = new PromptRepository(database);
            var images = new ImageStorageService(paths, database);

            var relativePath = await images.ImportAsync(sourcePath);
            var storedPath = images.ResolvePath(relativePath);
            Assert.True(File.Exists(storedPath));

            var prompt = new PromptItem
            {
                Category = PromptCategory.Character,
                Title = "Image owner",
                ImagePath = relativePath
            };
            await prompts.CreateAsync(prompt);

            await images.DeleteIfUnreferencedAsync(relativePath);
            Assert.True(File.Exists(storedPath));

            await prompts.DeleteAsync(prompt.Id);
            await images.DeleteIfUnreferencedAsync(relativePath);
            Assert.False(File.Exists(storedPath));
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
