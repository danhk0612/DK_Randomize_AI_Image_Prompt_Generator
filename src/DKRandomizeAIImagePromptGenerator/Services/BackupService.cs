using System.IO.Compression;
using DKRandomizeAIImagePromptGenerator.Data;
using Microsoft.Data.Sqlite;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class BackupService
{
    private readonly AppDataPaths _paths;

    public BackupService(AppDataPaths paths)
    {
        _paths = paths;
    }

    public Task CreateAsync(string destinationPath)
    {
        _paths.EnsureDirectories();
        SqliteConnection.ClearAllPools();

        using var stream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);

        AddFileIfExists(archive, _paths.DatabasePath, "data/prompts.db");
        AddFileIfExists(archive, _paths.SettingsPath, "settings.json");

        if (Directory.Exists(_paths.ImagesDirectory))
        {
            foreach (var imagePath in Directory.EnumerateFiles(_paths.ImagesDirectory))
            {
                archive.CreateEntryFromFile(
                    imagePath,
                    $"images/{Path.GetFileName(imagePath)}",
                    CompressionLevel.Optimal);
            }
        }

        return Task.CompletedTask;
    }

    public Task RestoreAsync(string sourcePath)
    {
        _paths.EnsureDirectories();
        SqliteConnection.ClearAllPools();

        using var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var databaseEntry = archive.GetEntry("data/prompts.db")
            ?? throw new InvalidDataException("올바른 DK Prompt Generator 백업이 아닙니다: data/prompts.db가 없습니다.");

        if (databaseEntry.Length == 0)
        {
            throw new InvalidDataException("백업의 데이터베이스 파일이 비어 있습니다.");
        }

        Directory.CreateDirectory(_paths.DataDirectory);
        databaseEntry.ExtractToFile(_paths.DatabasePath, overwrite: true);

        var settingsEntry = archive.GetEntry("settings.json");
        if (settingsEntry is not null)
        {
            settingsEntry.ExtractToFile(_paths.SettingsPath, overwrite: true);
        }

        if (Directory.Exists(_paths.ImagesDirectory))
        {
            Directory.Delete(_paths.ImagesDirectory, recursive: true);
        }
        Directory.CreateDirectory(_paths.ImagesDirectory);

        foreach (var entry in archive.Entries.Where(entry =>
                     entry.FullName.StartsWith("images/", StringComparison.Ordinal) &&
                     !string.IsNullOrWhiteSpace(entry.Name)))
        {
            var destination = Path.Combine(_paths.ImagesDirectory, entry.Name);
            entry.ExtractToFile(destination, overwrite: true);
        }

        return Task.CompletedTask;
    }

    private static void AddFileIfExists(
        ZipArchive archive,
        string sourcePath,
        string entryName)
    {
        if (File.Exists(sourcePath))
        {
            archive.CreateEntryFromFile(sourcePath, entryName, CompressionLevel.Optimal);
        }
    }
}
