using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.UI.Xaml;

namespace DKRandomizeAIImagePromptGenerator;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();

        var paths = AppDataPaths.CreateDefault();
        Database = new DatabaseService(paths);
        Prompts = new PromptRepository(Database);
        History = new HistoryRepository(Database);
        Images = new ImageStorageService(paths, Database);
        Combination = new CombinationService();
        Clipboard = new ClipboardService();
        Settings = new SettingsService(paths);
        Backup = new BackupService(paths);
    }

    public DatabaseService Database { get; }

    public PromptRepository Prompts { get; }

    public HistoryRepository History { get; }

    public ImageStorageService Images { get; }

    public CombinationService Combination { get; }

    public ClipboardService Clipboard { get; }

    public SettingsService Settings { get; }

    public BackupService Backup { get; }

    public Window? MainWindowInstance => _window;

    public CombinationHistory? PendingHistoryRestore { get; set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Database.InitializeAsync();
        await Settings.LoadAsync();

        var window = new MainWindow();
        window.ApplyTheme(Settings.Current.Theme);
        _window = window;
        _window.Activate();
    }
}
