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
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        _syncingTheme = true;
        ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
        _syncingTheme = false;
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
