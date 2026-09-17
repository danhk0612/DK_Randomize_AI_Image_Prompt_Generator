using DKRandomizeAIImagePromptGenerator.Data;
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
    }

    public DatabaseService Database { get; }

    public PromptRepository Prompts { get; }

    public HistoryRepository History { get; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Database.InitializeAsync();

        _window = new MainWindow();
        _window.Activate();
    }
}
