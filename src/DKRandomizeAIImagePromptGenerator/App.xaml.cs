using System.Runtime.InteropServices;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.UI.Xaml;

namespace DKRandomizeAIImagePromptGenerator;

public partial class App : Application
{
    private const string AppDataFolderName = "DK Randomize AI Image Prompt Generator";
    private const string StartupLogFileName = "startup-crash.log";

    private Window? _window;

    public App()
    {
        try
        {
            InitializeComponent();
            UnhandledException += App_UnhandledException;

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
        catch (Exception exception)
        {
            ReportStartupFailure("App constructor", exception);
            throw;
        }
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
        try
        {
            await Database.InitializeAsync();
            await Settings.LoadAsync();

            var window = new MainWindow();
            window.ApplyTheme(Settings.Current.Theme);
            _window = window;
            _window.Activate();
        }
        catch (Exception exception)
        {
            ReportStartupFailure("OnLaunched", exception);
            Environment.Exit(1);
        }
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        ReportStartupFailure("UnhandledException", e.Exception);
    }

    private static void ReportStartupFailure(string stage, Exception exception)
    {
        var logPath = GetStartupLogPath();
        var message = $"Stage: {stage}{Environment.NewLine}" +
                      $"Time: {DateTimeOffset.Now:O}{Environment.NewLine}" +
                      $"OS: {Environment.OSVersion}{Environment.NewLine}" +
                      $"Runtime: {Environment.Version}{Environment.NewLine}{Environment.NewLine}" +
                      exception;

        try
        {
            var directory = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(logPath, message);
        }
        catch
        {
        }

        try
        {
            MessageBoxW(
                IntPtr.Zero,
                $"프로그램 시작 중 오류가 발생했습니다.\n\n진단 로그:\n{logPath}\n\n{exception.Message}",
                "DK Randomize AI Image Prompt Generator",
                0x00000010);
        }
        catch
        {
        }
    }

    private static string GetStartupLogPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            StartupLogFileName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
