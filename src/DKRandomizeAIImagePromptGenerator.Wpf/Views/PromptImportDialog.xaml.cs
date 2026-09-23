using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using DKRandomizeAIImagePromptGenerator.Models;
using DKRandomizeAIImagePromptGenerator.Services;
using Microsoft.Win32;

namespace DKRandomizeAIImagePromptGenerator.Wpf.Views;

public partial class PromptImportDialog : Window
{
    private readonly PromptCategory _category;
    private readonly PromptImportService _importService;
    private readonly List<PromptItem> _importedItems = [];
    private bool _running;

    public PromptImportDialog(
        PromptCategory category,
        PromptImportService importService)
    {
        _category = category;
        _importService = importService;

        InitializeComponent();
        DataContext = this;
        CategoryText.Text = $"대상 분류: {GetCategoryName(category)}";
    }

    public ObservableCollection<PromptImportRow> Rows { get; } = [];

    public IReadOnlyList<PromptItem> ImportedItems => _importedItems;

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Title = "프롬프트 TXT 파일이 있는 폴더 선택",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var files = Directory
            .EnumerateFiles(dialog.FolderName, "*", SearchOption.TopDirectoryOnly)
            .Where(path => string.Equals(
                Path.GetExtension(path),
                ".txt",
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        LoadFiles(files);
    }

    private void ChooseFiles_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "프롬프트 TXT 파일 선택",
            Filter = "텍스트 파일|*.txt",
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LoadFiles(dialog.FileNames);
    }

    private void LoadFiles(IEnumerable<string> files)
    {
        Rows.Clear();
        _importedItems.Clear();
        ResultSummaryText.Text = string.Empty;

        foreach (var path in files
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase))
        {
            try
            {
                var preview = _importService.Inspect(path);
                Rows.Add(new PromptImportRow(
                    preview.SourcePath,
                    preview.Title,
                    Path.GetFileName(preview.SourcePath),
                    preview.ImageSourcePath is null
                        ? "없음"
                        : Path.GetFileName(preview.ImageSourcePath)));
            }
            catch (Exception ex)
            {
                Rows.Add(new PromptImportRow(
                    path,
                    Path.GetFileNameWithoutExtension(path),
                    Path.GetFileName(path),
                    "없음")
                {
                    Status = $"실패 - {ToShortMessage(ex)}",
                    CanImport = false
                });
            }
        }

        SelectionSummaryText.Text = $"선택된 파일 {Rows.Count}개";
        StartImportButton.IsEnabled = Rows.Any(row => row.CanImport);
    }

    private async void StartImport_Click(object sender, RoutedEventArgs e)
    {
        if (_running || Rows.Count == 0)
        {
            return;
        }

        _running = true;
        SetSelectionButtonsEnabled(false);
        StartImportButton.IsEnabled = false;
        _importedItems.Clear();

        var successCount = 0;
        var failureCount = Rows.Count(row => !row.CanImport);

        foreach (var row in Rows.Where(row => row.CanImport))
        {
            row.Status = "처리 중...";

            try
            {
                var item = await _importService.ImportAsync(
                    row.SourcePath,
                    _category);
                _importedItems.Add(item);
                row.Status = "완료";
                successCount++;
            }
            catch (Exception ex)
            {
                row.Status = $"실패 - {ToShortMessage(ex)}";
                failureCount++;
            }
        }

        ResultSummaryText.Text =
            $"가져오기 완료 · 성공 {successCount}개 / 실패 {failureCount}개 / 전체 {Rows.Count}개";

        _running = false;
        SetSelectionButtonsEnabled(true);
    }

    private void SetSelectionButtonsEnabled(bool enabled)
    {
        ChooseFolderButton.IsEnabled = enabled;
        ChooseFilesButton.IsEnabled = enabled;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!_running)
        {
            Close();
        }
    }

    private static string GetCategoryName(PromptCategory category) => category switch
    {
        PromptCategory.Artist => "작가 / 스타일",
        PromptCategory.Additional => "추가",
        _ => "캐릭터"
    };

    private static string ToShortMessage(Exception exception)
    {
        var message = exception.Message
            .Replace(Environment.NewLine, " ")
            .Trim();

        return message.Length <= 90
            ? message
            : message[..87] + "...";
    }
}

public sealed class PromptImportRow : INotifyPropertyChanged
{
    private string _status = "대기";

    public PromptImportRow(
        string sourcePath,
        string title,
        string fileName,
        string imageName)
    {
        SourcePath = sourcePath;
        Title = title;
        FileName = fileName;
        ImageName = imageName;
    }

    public string SourcePath { get; }

    public string Title { get; }

    public string FileName { get; }

    public string ImageName { get; }

    public bool CanImport { get; set; } = true;

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
