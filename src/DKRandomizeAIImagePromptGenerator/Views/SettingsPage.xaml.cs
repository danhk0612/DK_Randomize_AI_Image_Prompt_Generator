using DKRandomizeAIImagePromptGenerator.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace DKRandomizeAIImagePromptGenerator.Views;

public sealed partial class SettingsPage : Page
{
    private bool _syncingTheme;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += SettingsPage_Loaded;
        SizeChanged += SettingsPage_SizeChanged;
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveLayout(ActualWidth);

        var app = (App)Application.Current;
        _syncingTheme = true;
        ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
        _syncingTheme = false;

        var version = typeof(App).Assembly.GetName().Version;
        VersionText.Text = version is null
            ? "버전 정보 없음"
            : $"버전 {version.Major}.{version.Minor}.{version.Build}";
    }

    private void SettingsPage_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveLayout(e.NewSize.Width);
    }

    private void ApplyResponsiveLayout(double availableWidth)
    {
        if (ThemeComboBox.Parent is Grid themeGrid && themeGrid.ColumnDefinitions.Count >= 2)
        {
            EnsureTwoRows(themeGrid);

            if (availableWidth < 700)
            {
                themeGrid.ColumnSpacing = 0;
                themeGrid.RowSpacing = 12;
                themeGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                themeGrid.ColumnDefinitions[1].Width = new GridLength(0);

                Grid.SetRow(ThemeComboBox, 1);
                Grid.SetColumn(ThemeComboBox, 0);
                Grid.SetColumnSpan(ThemeComboBox, 2);
                ThemeComboBox.MaxWidth = double.PositiveInfinity;
            }
            else
            {
                themeGrid.ColumnSpacing = 20;
                themeGrid.RowSpacing = 0;
                themeGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                themeGrid.ColumnDefinitions[1].Width = new GridLength(220);

                Grid.SetRow(ThemeComboBox, 0);
                Grid.SetColumn(ThemeComboBox, 1);
                Grid.SetColumnSpan(ThemeComboBox, 1);
            }
        }

        if (BackupButtons.Parent is Grid backupGrid && backupGrid.ColumnDefinitions.Count >= 2)
        {
            EnsureTwoRows(backupGrid);

            if (availableWidth < 700)
            {
                backupGrid.ColumnSpacing = 0;
                backupGrid.RowSpacing = 12;
                backupGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                backupGrid.ColumnDefinitions[1].Width = new GridLength(0);

                Grid.SetRow(BackupButtons, 1);
                Grid.SetColumn(BackupButtons, 0);
                Grid.SetColumnSpan(BackupButtons, 2);
            }
            else
            {
                backupGrid.ColumnSpacing = 20;
                backupGrid.RowSpacing = 0;
                backupGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                backupGrid.ColumnDefinitions[1].Width = GridLength.Auto;

                Grid.SetRow(BackupButtons, 0);
                Grid.SetColumn(BackupButtons, 1);
                Grid.SetColumnSpan(BackupButtons, 1);
            }
        }
    }

    private static void EnsureTwoRows(Grid grid)
    {
        if (grid.RowDefinitions.Count >= 2)
        {
            return;
        }

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
    }

    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingTheme || ThemeComboBox.SelectedIndex < 0)
        {
            return;
        }

        var theme = (AppTheme)ThemeComboBox.SelectedIndex;
        var app = (App)Application.Current;
        await app.Settings.SetThemeAsync(theme);

        if (app.MainWindowInstance is MainWindow window)
        {
            window.ApplyTheme(theme);
        }
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        if (app.MainWindowInstance is not MainWindow window)
        {
            return;
        }

        var picker = new FileSavePicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"DK-Prompt-Backup-{DateTime.Now:yyyyMMdd-HHmmss}",
            CommitButtonText = "백업",
            DefaultFileExtension = ".zip",
            FileTypeChoices =
            {
                { "DK Prompt Generator Backup", new List<string> { ".zip" } }
            }
        };

        var result = await picker.PickSaveFileAsync();
        if (result is null)
        {
            return;
        }

        await app.Backup.CreateAsync(result.Path);
        ShowStatus("백업 완료", "로컬 데이터 백업 파일을 저장했습니다.", InfoBarSeverity.Success);
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        if (app.MainWindowInstance is not MainWindow window)
        {
            return;
        }

        var picker = new FileOpenPicker(window.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            CommitButtonText = "복원",
            ViewMode = PickerViewMode.List,
            FileTypeFilter = { ".zip" }
        };

        var result = await picker.PickSingleFileAsync();
        if (result is null)
        {
            return;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "백업 복원",
            Content = "현재 프롬프트, 최근 기록, 대표 이미지와 설정을 선택한 백업 내용으로 교체합니다.",
            PrimaryButtonText = "복원",
            CloseButtonText = "취소",
            DefaultButton = ContentDialogButton.Close
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        await app.Backup.RestoreAsync(result.Path);
        await app.Database.InitializeAsync();
        await app.Settings.LoadAsync();
        window.ApplyTheme(app.Settings.Current.Theme);

        _syncingTheme = true;
        ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
        _syncingTheme = false;

        ShowStatus("복원 완료", "백업 데이터를 복원했습니다. 다른 화면으로 이동하면 복원된 데이터가 표시됩니다.", InfoBarSeverity.Success);
    }

    private void ShowStatus(string title, string message, InfoBarSeverity severity)
    {
        StatusInfoBar.Title = title;
        StatusInfoBar.Message = message;
        StatusInfoBar.Severity = severity;
        StatusInfoBar.IsOpen = true;
    }
}
