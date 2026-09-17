using System.Windows;
using System.Windows.Controls;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Wpf.Services;
using Microsoft.Win32;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class SettingsView : UserControl
{
    private bool _syncingTheme;

    public SettingsView()
    {
        InitializeComponent();
        WheelScrollService.Enable(RootScrollViewer);
        Loaded += SettingsView_Loaded;
    }

    private void SettingsView_Loaded(object sender, RoutedEventArgs e)
    {
        var app = (App)Application.Current;
        _syncingTheme = true;
        ThemeComboBox.SelectedIndex = (int)app.Settings.Current.Theme;
        _syncingTheme = false;

        var version = typeof(App).Assembly.GetName().Version;
        VersionText.Text = version is null
            ? "버전 정보 없음"
            : $"버전 {version.Major}.{version.Minor}.{version.Build}";
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
