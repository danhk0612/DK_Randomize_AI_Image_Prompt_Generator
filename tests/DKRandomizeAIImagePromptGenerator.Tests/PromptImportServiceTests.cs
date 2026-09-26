using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.Data.Sqlite;
using Xunit;

namespace DKRandomizeAIImagePromptGenerator.Tests;

public sealed class PromptImportServiceTests
{
    [Fact]
    public async Task ImportsPositivePromptOptionalFieldsAndMatchingImage()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "stellive_rin.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                1girl, long hair, blue eyes

                [Tags]
                character, stellive, rin

                [Memo]
                캐릭터 기본 프롬프트
                """);
            await File.WriteAllBytesAsync(
                Path.Combine(input, "stellive_rin.PNG"),
                [1, 2, 3, 4]);

            var (repository, images, service) = await CreateServicesAsync(root);
            var item = await service.ImportAsync(txtPath, PromptCategory.Character);

            Assert.Equal("stellive_rin", item.Title);
            Assert.Equal("1girl, long hair, blue eyes", item.PositivePrompt);
            Assert.Equal(string.Empty, item.NegativePrompt);
            Assert.Equal("캐릭터 기본 프롬프트", item.Memo);
            Assert.Equal(["character", "stellive", "rin"], item.Tags);
            Assert.NotNull(item.ImagePath);
            Assert.True(File.Exists(images.ResolvePath(item.ImagePath!)));

            var stored = await repository.GetByIdAsync(item.Id);
            Assert.NotNull(stored);
            Assert.Equal(item.Title, stored.Title);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task ImportsNegativeOnlyWithoutOptionalFields()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "negative_only.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Negative]
                blurry, low quality
                """);

            var (_, _, service) = await CreateServicesAsync(root);
            var item = await service.ImportAsync(txtPath, PromptCategory.Additional);

            Assert.Equal("negative_only", item.Title);
            Assert.Equal(string.Empty, item.PositivePrompt);
            Assert.Equal("blurry, low quality", item.NegativePrompt);
            Assert.Empty(item.Tags);
            Assert.Equal(string.Empty, item.Memo);
            Assert.Null(item.ImagePath);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task RejectsFileWithoutPositiveOrNegativePrompt()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "invalid.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Tags]
                test

                [Memo]
                no prompt
                """);

            var (_, _, service) = await CreateServicesAsync(root);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(
                () => service.ImportAsync(txtPath, PromptCategory.Character));

            Assert.Contains("Positive 또는 Negative", exception.Message);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task RejectsDuplicateTitleWithinSameCategory()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "duplicate.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                first
                """);

            var (_, _, service) = await CreateServicesAsync(root);
            await service.ImportAsync(txtPath, PromptCategory.Character);

            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                second
                """);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.ImportAsync(txtPath, PromptCategory.Character));

            Assert.Contains("동일한 제목", exception.Message);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task ForceMergeOverwritesExistingPromptWithSameTitle()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "duplicate.txt");
            var imagePath = Path.Combine(input, "duplicate.png");

            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                original positive

                [Tags]
                original

                [Memo]
                original memo
                """);
            await File.WriteAllBytesAsync(imagePath, [1, 2, 3, 4]);

            var (repository, images, service) = await CreateServicesAsync(root);
            var original = await service.ImportAsync(
                txtPath,
                PromptCategory.Character);
            var originalId = original.Id;
            var originalCreatedAt = original.CreatedAt;
            var originalStoredImage = original.ImagePath;
            Assert.NotNull(originalStoredImage);
            Assert.True(File.Exists(images.ResolvePath(originalStoredImage!)));

            File.Delete(imagePath);
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Negative]
                replacement negative

                [Tags]
                replacement
                """);

            var overwritten = await service.ImportAsync(
                txtPath,
                PromptCategory.Character,
                PromptImportConflictMode.Overwrite);

            Assert.Equal(originalId, overwritten.Id);
            Assert.Equal(originalCreatedAt, overwritten.CreatedAt);
            Assert.Equal(string.Empty, overwritten.PositivePrompt);
            Assert.Equal("replacement negative", overwritten.NegativePrompt);
            Assert.Equal(["replacement"], overwritten.Tags);
            Assert.Equal(string.Empty, overwritten.Memo);
            Assert.Null(overwritten.ImagePath);
            Assert.False(File.Exists(images.ResolvePath(originalStoredImage!)));

            var stored = await repository.GetByTitleAsync(
                PromptCategory.Character,
                "duplicate");
            Assert.NotNull(stored);
            Assert.Equal(originalId, stored.Id);
            Assert.Equal("replacement negative", stored.NegativePrompt);
            Assert.Equal(["replacement"], stored.Tags);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task RenameModeAddsNumberedPromptWithoutChangingExistingItems()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "duplicate.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                original
                """);

            var (repository, _, service) = await CreateServicesAsync(root);
            var original = await service.ImportAsync(
                txtPath,
                PromptCategory.Character);

            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                renamed copy
                """);

            var second = await service.ImportAsync(
                txtPath,
                PromptCategory.Character,
                PromptImportConflictMode.Rename);
            var third = await service.ImportAsync(
                txtPath,
                PromptCategory.Character,
                PromptImportConflictMode.Rename);

            Assert.Equal("duplicate", original.Title);
            Assert.Equal("duplicate (2)", second.Title);
            Assert.Equal("duplicate (3)", third.Title);
            Assert.NotEqual(original.Id, second.Id);
            Assert.NotEqual(second.Id, third.Id);

            var storedOriginal = await repository.GetByIdAsync(original.Id);
            Assert.NotNull(storedOriginal);
            Assert.Equal("original", storedOriginal.PositivePrompt);

            var storedSecond = await repository.GetByIdAsync(second.Id);
            Assert.NotNull(storedSecond);
            Assert.Equal("renamed copy", storedSecond.PositivePrompt);
        }
        finally
        {
            Cleanup(root);
        }
    }

    [Fact]
    public async Task SameTitleCanBeImportedIntoDifferentCategories()
    {
        var root = CreateTemporaryRoot();

        try
        {
            var input = Path.Combine(root, "input");
            Directory.CreateDirectory(input);

            var txtPath = Path.Combine(input, "shared_title.txt");
            await File.WriteAllTextAsync(
                txtPath,
                """
                [Positive]
                reusable prompt
                """);

            var (_, _, service) = await CreateServicesAsync(root);

            var character = await service.ImportAsync(
                txtPath,
                PromptCategory.Character);
            var artist = await service.ImportAsync(
                txtPath,
                PromptCategory.Artist);

            Assert.Equal(character.Title, artist.Title);
            Assert.NotEqual(character.Category, artist.Category);
        }
        finally
        {
            Cleanup(root);
        }
    }

    private static async Task<(
        PromptRepository Repository,
        ImageStorageService Images,
        PromptImportService Service)> CreateServicesAsync(string root)
    {
        var paths = AppDataPaths.Create(Path.Combine(root, "appdata"));
        var database = new DatabaseService(paths);
        await database.InitializeAsync();

        var repository = new PromptRepository(database);
        var images = new ImageStorageService(paths, database);
        return (
            repository,
            images,
            new PromptImportService(repository, images));
    }

    private static string CreateTemporaryRoot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DKRandomizeAIImagePromptGenerator.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
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
