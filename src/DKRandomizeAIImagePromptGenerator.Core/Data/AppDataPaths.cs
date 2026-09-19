namespace DKRandomizeAIImagePromptGenerator.Data;

public sealed record AppDataPaths(
    string RootDirectory,
    string DataDirectory,
    string DatabasePath,
    string ImagesDirectory,
    string BackupsDirectory,
    string SettingsPath)
{
    public const string PortableModeMarkerFileName = "portable.mode";

    public static AppDataPaths CreateDefault() =>
        Create(IsPortableModeEnabled()
            ? GetExecutableRootDirectory()
            : GetLocalRootDirectory());

    public static string GetLocalRootDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DK Randomize AI Image Prompt Generator");

    public static string GetExecutableRootDirectory(string? executableDirectory = null) =>
        Path.GetFullPath(executableDirectory ?? AppContext.BaseDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    public static string GetPortableModeMarkerPath(string? executableDirectory = null) =>
        Path.Combine(
            GetExecutableRootDirectory(executableDirectory),
            PortableModeMarkerFileName);

    public static bool IsPortableModeEnabled(string? executableDirectory = null) =>
        File.Exists(GetPortableModeMarkerPath(executableDirectory));

    public static void SetPortableModeEnabled(
        bool enabled,
        string? executableDirectory = null)
    {
        var markerPath = GetPortableModeMarkerPath(executableDirectory);

        if (enabled)
        {
            File.WriteAllText(
                markerPath,
                "DK Randomize AI Image Prompt Generator portable data mode");
            return;
        }

        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
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
