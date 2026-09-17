using DKRandomizeAIImagePromptGenerator.Data;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class ImageStorageService
{
    private readonly AppDataPaths _paths;

    public ImageStorageService(AppDataPaths paths)
    {
        _paths = paths;
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

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
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
