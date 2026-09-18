using System.IO;
using System.Windows;
using System.Windows.Media;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;
using Microsoft.Win32;

namespace DKRandomizeAIImagePromptGenerator.Wpf;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Any(arg => string.Equals(arg, "--scroll-smoke", StringComparison.OrdinalIgnoreCase)))
        {
            var succeeded = ScrollSmokeRunner.Run();
            Shutdown(succeeded ? 0 : 2);
            return;
        }

        if (e.Args.Any(arg => string.Equals(arg, "--ui-smoke", StringComparison.OrdinalIgnoreCase)))
        {
            var succeeded = await UiSmokeRunner.RunAsync(this);
            Shutdown(succeeded ? 0 : 3);
            return;
        }

        var paths = AppDataPaths.CreateDefault();

        try
        {
            await InitializeServicesAsync(paths);
            ClearStartupCrashLog(paths);

            MainWindowInstance = new MainWindow();
            MainWindow = MainWindowInstance;
            MainWindowInstance.Show();
        }
        catch (Exception ex)
        {
            WriteStartupCrashLog(paths, ex);
            MessageBox.Show(
                $"프로그램 시작 중 오류가 발생했습니다.\n\n{ex}",
                "DK Randomize AI Image Prompt Generator",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    internal async Task InitializeServicesAsync(AppDataPaths paths)
    {
        Database = new DatabaseService(paths);
        Prompts = new PromptRepository(Database);
        History = new HistoryRepository(Database);
        Images = new ImageStorageService(paths, Database);
        Combination = new CombinationService();
        Settings = new SettingsService(paths);
        Backup = new BackupService(paths);

        await Database.InitializeAsync();
        await Settings.LoadAsync();
        ApplyTheme(Settings.Current.Theme);
    }

    private static string GetStartupCrashLogPath(AppDataPaths paths) =>
        Path.Combine(paths.RootDirectory, "startup-crash.log");

    private static void ClearStartupCrashLog(AppDataPaths paths)
    {
        try
        {
            var path = GetStartupCrashLogPath(paths);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // A stale diagnostics file must never prevent startup.
        }
    }

    private static void WriteStartupCrashLog(AppDataPaths paths, Exception exception)
    {
        try
        {
            paths.EnsureDirectories();
            File.WriteAllText(
                GetStartupCrashLogPath(paths),
                $"[{DateTimeOffset.Now:O}] WPF startup failure{Environment.NewLine}{exception}");
        }
        catch
        {
            // Fall back to the startup error dialog if logging itself fails.
        }
    }

    public DatabaseService Database { get; private set; } = null!;
    public PromptRepository Prompts { get; private set; } = null!;
    public HistoryRepository History { get; private set; } = null!;
    public ImageStorageService Images { get; private set; } = null!;
    public CombinationService Combination { get; private set; } = null!;
    public SettingsService Settings { get; private set; } = null!;
    public BackupService Backup { get; private set; } = null!;
    public MainWindow? MainWindowInstance { get; private set; }
    public CombinationHistory? PendingHistoryRestore { get; set; }

    public void ApplyTheme(AppTheme theme)
    {
        var dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDarkTheme()
        };

        SetBrush("WindowBackgroundBrush", dark ? "#181A1F" : "#F5F6F8");
        SetBrush("PaneBackgroundBrush", dark ? "#202329" : "#ECEEF2");
        SetBrush("SurfaceBrush", dark ? "#25282E" : "#FFFFFF");
        SetBrush("SurfaceSecondaryBrush", dark ? "#2D3037" : "#F1F2F4");
        SetBrush("BorderBrush", dark ? "#3A3E47" : "#D9DCE2");
        SetBrush("TextPrimaryBrush", dark ? "#F3F4F6" : "#1F2328");
        SetBrush("TextSecondaryBrush", dark ? "#AEB4BF" : "#666D77");
    }

    private void SetBrush(string key, string color)
    {
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
