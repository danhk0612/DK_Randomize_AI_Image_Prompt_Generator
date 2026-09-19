using System.Text.Json;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed class SettingsService
{
    private readonly AppDataPaths _paths;

    public SettingsService(AppDataPaths paths)
    {
        _paths = paths;
    }

    public AppSettings Current { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureDirectories();

        if (!File.Exists(_paths.SettingsPath))
        {
            Current = new AppSettings();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.SettingsPath);
            Current = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                cancellationToken: cancellationToken) ?? new AppSettings();
        }
        catch (JsonException)
        {
            PreserveCorruptSettingsFile();
            Current = new AppSettings();
            await SaveAsync(cancellationToken);
        }
    }

    private void PreserveCorruptSettingsFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_paths.SettingsPath);
            var fileName = Path.GetFileNameWithoutExtension(_paths.SettingsPath);
            var extension = Path.GetExtension(_paths.SettingsPath);
            var backupName = $"{fileName}.corrupt-{DateTime.Now:yyyyMMdd-HHmmssfff}{extension}";
            var backupPath = Path.Combine(directory ?? _paths.RootDirectory, backupName);
            File.Move(_paths.SettingsPath, backupPath, overwrite: true);
        }
        catch
        {
            // Recovery must not fail only because the damaged file could not be renamed.
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        _paths.EnsureDirectories();
        await using var stream = File.Create(_paths.SettingsPath);
        await JsonSerializer.SerializeAsync(
            stream,
            Current,
            new JsonSerializerOptions { WriteIndented = true },
            cancellationToken);
    }

    public void Save()
    {
        _paths.EnsureDirectories();
        using var stream = File.Create(_paths.SettingsPath);
        JsonSerializer.Serialize(
            stream,
            Current,
            new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task SetThemeAsync(
        AppTheme theme,
        CancellationToken cancellationToken = default)
    {
        Current.Theme = theme;
        await SaveAsync(cancellationToken);
    }
}
