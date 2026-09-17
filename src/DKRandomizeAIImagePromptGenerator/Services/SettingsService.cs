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

        await using var stream = File.OpenRead(_paths.SettingsPath);
        Current = await JsonSerializer.DeserializeAsync<AppSettings>(
            stream,
            cancellationToken: cancellationToken) ?? new AppSettings();
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

    public async Task SetThemeAsync(
        AppTheme theme,
        CancellationToken cancellationToken = default)
    {
        Current.Theme = theme;
        await SaveAsync(cancellationToken);
    }
}
