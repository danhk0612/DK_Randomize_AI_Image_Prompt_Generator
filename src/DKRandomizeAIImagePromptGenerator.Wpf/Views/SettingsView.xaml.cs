using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;
using Microsoft.Win32;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class SettingsView : UserControl
{
    private bool _syncingTheme;
    private bool _syncingStorage;

    public SettingsView()
    {
        InitializeComponent();
        WheelScrollService.Enable(RootScrollViewer);
        Loaded += SettingsView_Loaded;
        SizeChanged += SettingsView_SizeChanged;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveLayout(ActualWidth);

        var app = (App)Application.Current;
        _syncingTheme = true;
        ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
        _syncingTheme = false;

        _syncingStorage = true;
        StorageLocationComboBox.SelectedIndex = AppDataPaths.IsPortableModeEnabled() ? 1 : 0;
        _syncingStorage = false;
        UpdateDataPathText(app);

        var version = GetCurrentVersion();
        VersionText.Text = string.IsNullOrWhiteSpace(version)
            ? "버전 정보 없음"
            : $"버전 {version}";
        UpdateStatusText.Text = string.IsNullOrWhiteSpace(version)
            ? "현재 버전을 확인할 수 없습니다."
            : $"현재 버전: {version}";
    }

    private void SettingsView_SizeChanged(object sender, SizeChangedEventArgs e) =>
        ApplyResponsiveLayout(e.NewSize.Width);

    private void ApplyResponsiveLayout(double availableWidth)
    {
        if (availableWidth < 700)
        {
            ThemeGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            ThemeGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(ThemeComboBox, 1);
            Grid.SetColumn(ThemeComboBox, 0);
            Grid.SetColumnSpan(ThemeComboBox, 2);
            ThemeComboBox.Margin = new Thickness(0, 12, 0, 0);

            StorageGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            StorageGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(StorageLocationComboBox, 1);
            Grid.SetColumn(StorageLocationComboBox, 0);
            Grid.SetColumnSpan(StorageLocationComboBox, 2);
            StorageLocationComboBox.Margin = new Thickness(0, 12, 0, 0);

            BackupGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            BackupGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(BackupButtons, 1);
            Grid.SetColumn(BackupButtons, 0);
            Grid.SetColumnSpan(BackupButtons, 2);
            BackupButtons.Margin = new Thickness(0, 12, 0, 0);

            UpdateGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            UpdateGrid.ColumnDefinitions[1].Width = new GridLength(0);
            Grid.SetRow(UpdateButton, 1);
            Grid.SetColumn(UpdateButton, 0);
            Grid.SetColumnSpan(UpdateButton, 2);
            UpdateButton.HorizontalAlignment = HorizontalAlignment.Left;
            UpdateButton.Margin = new Thickness(0, 12, 0, 0);
        }
        else
        {
            ThemeGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            ThemeGrid.ColumnDefinitions[1].Width = new GridLength(220);
            Grid.SetRow(ThemeComboBox, 0);
            Grid.SetColumn(ThemeComboBox, 1);
            Grid.SetColumnSpan(ThemeComboBox, 1);
            ThemeComboBox.Margin = new Thickness(0);

            StorageGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            StorageGrid.ColumnDefinitions[1].Width = new GridLength(220);
            Grid.SetRow(StorageLocationComboBox, 0);
            Grid.SetColumn(StorageLocationComboBox, 1);
            Grid.SetColumnSpan(StorageLocationComboBox, 1);
            StorageLocationComboBox.Margin = new Thickness(0);

            BackupGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            BackupGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            Grid.SetRow(BackupButtons, 0);
            Grid.SetColumn(BackupButtons, 1);
            Grid.SetColumnSpan(BackupButtons, 1);
            BackupButtons.Margin = new Thickness(16, 0, 0, 0);

            UpdateGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            UpdateGrid.ColumnDefinitions[1].Width = GridLength.Auto;
            Grid.SetRow(UpdateButton, 0);
            Grid.SetColumn(UpdateButton, 1);
            Grid.SetColumnSpan(UpdateButton, 1);
            UpdateButton.HorizontalAlignment = HorizontalAlignment.Right;
            UpdateButton.Margin = new Thickness(16, 0, 0, 0);
        }
    }

    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingTheme || ThemeComboBox.SelectedIndex < 0) return;

        try
        {
            var theme = (AppTheme)ThemeComboBox.SelectedIndex;
            var app = (App)Application.Current;
            await app.Settings.SetThemeAsync(theme);
            app.ApplyTheme(theme);
            StatusText.Text = "테마 설정을 저장했습니다.";
        }
        catch (Exception ex)
        {
            ShowError("테마 저장 실패", ex);
        }
    }

    private void StorageLocationComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_syncingStorage || StorageLocationComboBox.SelectedIndex < 0)
        {
            return;
        }

        var portable = StorageLocationComboBox.SelectedIndex == 1;
        var currentPortable = AppDataPaths.IsPortableModeEnabled();
        if (portable == currentPortable)
        {
            return;
        }

        var locationName = portable
            ? "실행 파일 폴더 (Portable)"
            : "사용자 데이터 폴더(LocalAppData)";

        if (MessageBox.Show(
                $"데이터 저장 위치를 '{locationName}'로 변경합니다.\n\n" +
                "변경은 다음 실행부터 적용되며 기존 데이터는 자동 이동하지 않습니다. " +
                "필요하면 변경 전에 백업을 만들어 두세요.\n\n계속할까요?",
                "데이터 저장 위치 변경",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            _syncingStorage = true;
            StorageLocationComboBox.SelectedIndex = currentPortable ? 1 : 0;
            _syncingStorage = false;
            return;
        }

        try
        {
            AppDataPaths.SetPortableModeEnabled(portable);
            var app = (App)Application.Current;
            UpdateDataPathText(app);
            StatusText.Text = "데이터 저장 위치를 변경했습니다. 다음 실행부터 새 위치를 사용합니다.";
        }
        catch (Exception ex)
        {
            _syncingStorage = true;
            StorageLocationComboBox.SelectedIndex = currentPortable ? 1 : 0;
            _syncingStorage = false;
            ShowError("데이터 저장 위치 변경 실패", ex);
        }
    }

    private void UpdateDataPathText(App app)
    {
        var nextRoot = AppDataPaths.IsPortableModeEnabled()
            ? AppDataPaths.GetExecutableRootDirectory()
            : AppDataPaths.GetLocalRootDirectory();

        var currentRoot = Path.GetFullPath(app.DataPaths.RootDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        nextRoot = Path.GetFullPath(nextRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        CurrentDataPathText.Text = string.Equals(
                currentRoot,
                nextRoot,
                StringComparison.OrdinalIgnoreCase)
            ? $"현재 위치: {currentRoot}"
            : $"현재 위치: {currentRoot}\n다음 실행 위치: {nextRoot}";
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        var currentVersion = GetCurrentVersion();
        if (string.IsNullOrWhiteSpace(currentVersion))
        {
            UpdateStatusText.Text = "현재 버전을 확인할 수 없어 업데이트를 진행할 수 없습니다.";
            return;
        }

        UpdateButton.IsEnabled = false;
        var updateStarted = false;

        try
        {
            var app = (App)Application.Current;
            UpdateStatusText.Text = "GitHub Releases에서 업데이트를 확인하고 있습니다...";

            var release = await app.Updates.CheckForUpdateAsync(currentVersion);
            if (release is null)
            {
                UpdateStatusText.Text = "현재 설치된 버전보다 새로운 GitHub Release가 없습니다.";
                return;
            }

            var sizeMb = release.AssetSize / 1024d / 1024d;
            UpdateStatusText.Text =
                $"새 버전 {release.Version} 사용 가능 ({sizeMb:0.0} MB)";

            var prereleaseText = release.IsPrerelease ? "\n이 버전은 사전 릴리스입니다." : string.Empty;
            if (MessageBox.Show(
                    $"새 버전 {release.Version}을(를) 설치합니다.\n\n" +
                    $"GitHub Release: {release.DisplayName}\n" +
                    $"다운로드: {release.AssetName} ({sizeMb:0.0} MB)" +
                    prereleaseText +
                    "\n\n다운로드 후 프로그램을 종료하고 파일을 교체한 뒤 자동으로 다시 실행합니다. 계속할까요?",
                    "프로그램 업데이트",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information) != MessageBoxResult.Yes)
            {
                UpdateStatusText.Text = $"업데이트 {release.Version} 설치를 취소했습니다.";
                return;
            }

            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
            {
                throw new InvalidOperationException("현재 실행 파일 경로를 확인할 수 없습니다.");
            }

            UpdateStatusText.Text = $"업데이트 {release.Version} 다운로드 중...";
            await app.Updates.DownloadAndStartUpdateAsync(
                release,
                executablePath,
                Environment.ProcessId);

            updateStarted = true;
            UpdateStatusText.Text = "업데이트 적용을 시작합니다. 프로그램을 종료합니다.";
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            ShowError("업데이트 실패", ex);
        }
        finally
        {
            if (!updateStarted)
            {
                UpdateButton.IsEnabled = true;
            }
        }
    }

    private static string GetCurrentVersion()
    {
        var informationalVersion = typeof(App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return string.Empty;
        }

        var metadataIndex = informationalVersion.IndexOf('+');
        return metadataIndex >= 0
            ? informationalVersion[..metadataIndex]
            : informationalVersion;
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new SaveFileDialog
            {
                Title = "데이터 백업",
                FileName = $"DK-Prompt-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
                DefaultExt = ".zip",
                Filter = "DK Prompt Generator Backup|*.zip"
            };

            if (dialog.ShowDialog() != true) return;
            await ((App)Application.Current).Backup.CreateAsync(dialog.FileName);
            StatusText.Text = "로컬 데이터 백업 파일을 저장했습니다.";
        }
        catch (Exception ex)
        {
            ShowError("백업 실패", ex);
        }
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "백업 복원",
                Filter = "DK Prompt Generator Backup|*.zip",
                Multiselect = false
            };
            if (dialog.ShowDialog() != true) return;

            if (MessageBox.Show(
                    "현재 프롬프트, 최근 기록, 대표 이미지와 설정을 선택한 백업 내용으로 교체합니다.",
                    "백업 복원",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            var app = (App)Application.Current;
            await app.Backup.RestoreAsync(dialog.FileName);
            await app.Database.InitializeAsync();
            await app.Settings.LoadAsync();
            app.ApplyTheme(app.Settings.Current.Theme);

            _syncingTheme = true;
            ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
            _syncingTheme = false;
            StatusText.Text = "백업 데이터를 복원했습니다. 다른 화면으로 이동하면 복원된 데이터가 표시됩니다.";
        }
        catch (Exception ex)
        {
            ShowError("복원 실패", ex);
        }
    }

    private void ShowError(string title, Exception ex)
    {
        StatusText.Text = $"{title}: {ex.Message}";
        MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
