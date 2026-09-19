using DKRandomizeAIImagePromptGenerator.Data;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class ImageStorageService
{
    private readonly AppDataPaths _paths;
    private readonly DatabaseService _database;

    public ImageStorageService(AppDataPaths paths, DatabaseService database)
    {
        _paths = paths;
        _database = database;
    }

    public Task<string> ImportAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _paths.EnsureDirectories();

        var extension = Path.GetExtension(sourcePath);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(_paths.ImagesDirectory, fileName);
        File.Copy(sourcePath, destinationPath, overwrite: false);

        var relativePath = Path.Combine("images", fileName);
        return Task.FromResult(relativePath);
    }

    public string ResolvePath(string relativePath) =>
        Path.GetFullPath(Path.Combine(_paths.RootDirectory, relativePath));

    public async Task DeleteIfUnreferencedAsync(
        string? relativePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        await using var connection = await _database.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM PromptItems WHERE ImagePath = @imagePath;";
        command.Parameters.AddWithValue("@imagePath", relativePath);
        var referenceCount = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

        if (referenceCount != 0)
        {
            return;
        }

        var path = ResolvePath(relativePath);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
