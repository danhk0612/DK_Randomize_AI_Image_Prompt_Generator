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
    }

    public DatabaseService Database { get; }

    public PromptRepository Prompts { get; }

    public HistoryRepository History { get; }

    public ImageStorageService Images { get; }

    public CombinationService Combination { get; }

    public ClipboardService Clipboard { get; }

    public Window? MainWindowInstance => _window;

    public CombinationHistory? PendingHistoryRestore { get; set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Database.InitializeAsync();

        _window = new MainWindow();
        _window.Activate();
    }
}
