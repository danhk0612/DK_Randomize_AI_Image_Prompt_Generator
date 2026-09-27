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

    private async void ChooseFolder_Click(object sender, RoutedEventArgs e)
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

        await LoadFilesAsync(files);
    }

    private async void ChooseFiles_Click(object sender, RoutedEventArgs e)
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

        await LoadFilesAsync(dialog.FileNames);
    }

    private async Task LoadFilesAsync(IEnumerable<string> files)
    {
        _running = true;
        SetSelectionButtonsEnabled(false);
        StartImportButton.IsEnabled = false;
        Rows.Clear();
        _importedItems.Clear();
        ResultSummaryText.Text = string.Empty;
        SelectionSummaryText.Text = "중복 / 유사 여부 검사 중...";

        try
        {
            var app = (App)Application.Current;
            var comparisonPool = (await app.Prompts.SearchAsync(_category)).ToList();

            foreach (var path in files
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.CurrentCultureIgnoreCase))
            {
                try
                {
                    var preview = await _importService.InspectContentAsync(path);
                    var candidate = new PromptItem
                    {
                        Category = _category,
                        Title = preview.Title,
                        PositivePrompt = preview.PositivePrompt,
                        NegativePrompt = preview.NegativePrompt
                    };

                    var match = app.PromptDuplicates.FindBestMatch(
                        candidate.PositivePrompt,
                        candidate.NegativePrompt,
                        comparisonPool);

                    var row = new PromptImportRow(
                        preview.SourcePath,
                        preview.Title,
                        Path.GetFileName(preview.SourcePath),
                        preview.ImageSourcePath is null
                            ? "없음"
                            : Path.GetFileName(preview.ImageSourcePath));

                    if (match is { IsExact: true })
                    {
                        row.Status = $"제외 - 완전 중복: {match.Item.Title}";
                        row.CanImport = false;
                        row.IsExactDuplicate = true;
                    }
                    else if (match is not null)
                    {
                        row.Status =
                            $"주의 - {match.Similarity:P0} 유사: {match.Item.Title}";
                        row.IsSimilarMatch = true;
                    }

                    Rows.Add(row);

                    if (!row.IsExactDuplicate)
                    {
                        comparisonPool.Add(candidate);
                    }
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

            var exactCount = Rows.Count(row => row.IsExactDuplicate);
            var similarCount = Rows.Count(row => row.IsSimilarMatch);
            SelectionSummaryText.Text =
                $"선택된 파일 {Rows.Count}개 · 완전 중복 {exactCount}개 제외 · 매우 유사 {similarCount}개";
            StartImportButton.IsEnabled = Rows.Any(row => row.CanImport);
        }
        finally
        {
            _running = false;
            SetSelectionButtonsEnabled(true);
        }
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
        var excludedCount = Rows.Count(row => row.IsExactDuplicate);
        var failureCount = Rows.Count(row => !row.CanImport && !row.IsExactDuplicate);
        var conflictMode = GetConflictMode();

        foreach (var row in Rows.Where(row => row.CanImport))
        {
            row.Status = "처리 중...";

            try
            {
                var item = await _importService.ImportAsync(
                    row.SourcePath,
                    _category,
                    conflictMode);
                _importedItems.Add(item);
                row.Status = string.Equals(
                    item.Title,
                    row.Title,
                    StringComparison.Ordinal)
                    ? "완료"
                    : $"완료 - {item.Title}";
                successCount++;
            }
            catch (Exception ex)
            {
                row.Status = $"실패 - {ToShortMessage(ex)}";
                failureCount++;
            }
        }

        ResultSummaryText.Text =
            $"가져오기 완료 · 성공 {successCount}개 / 중복 제외 {excludedCount}개 / 실패 {failureCount}개 / 전체 {Rows.Count}개";

        _running = false;
        SetSelectionButtonsEnabled(true);
    }

    private void SetSelectionButtonsEnabled(bool enabled)
    {
        ChooseFolderButton.IsEnabled = enabled;
        ChooseFilesButton.IsEnabled = enabled;
        ConflictModePanel.IsEnabled = enabled;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        if (!_running)
        {
            Close();
        }
    }

    private PromptImportConflictMode GetConflictMode()
    {
        if (ForceMergeModeRadioButton.IsChecked == true)
        {
            return PromptImportConflictMode.Overwrite;
        }

        if (RenameModeRadioButton.IsChecked == true)
        {
            return PromptImportConflictMode.Rename;
        }

        return PromptImportConflictMode.Fail;
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

    public bool IsExactDuplicate { get; set; }

    public bool IsSimilarMatch { get; set; }

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
