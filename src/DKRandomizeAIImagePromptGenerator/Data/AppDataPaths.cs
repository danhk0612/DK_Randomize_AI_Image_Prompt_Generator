namespace DKRandomizeAIImagePromptGenerator.Data;

public sealed record AppDataPaths(
    string RootDirectory,
    string DataDirectory,
    string DatabasePath,
    string ImagesDirectory,
    string BackupsDirectory,
    string SettingsPath)
{
    public static AppDataPaths CreateDefault()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DK Randomize AI Image Prompt Generator");

        return Create(root);
    }

    public static AppDataPaths Create(string rootDirectory)
    {
        var dataDirectory = Path.Combine(rootDirectory, "data");

        return new AppDataPaths(
            rootDirectory,
            dataDirectory,
            Path.Combine(dataDirectory, "prompts.db"),
            Path.Combine(rootDirectory, "images"),
            Path.Combine(rootDirectory, "backups"),
            Path.Combine(rootDirectory, "settings.json"));
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(ImagesDirectory);
        Directory.CreateDirectory(BackupsDirectory);
    }
}
