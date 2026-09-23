using System.Text;
using DKRandomizeAIImagePromptGenerator.Data;
using DKRandomizeAIImagePromptGenerator.Models;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed record PromptImportPreview(
    string SourcePath,
    string Title,
    string? ImageSourcePath);

public sealed class PromptImportService
{
    private static readonly string[] SupportedImageExtensions =
    [
        ".png",
        ".webp",
        ".jpg",
        ".jpeg",
        ".bmp"
    ];

    private static readonly UTF8Encoding StrictUtf8 =
        new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly PromptRepository _prompts;
    private readonly ImageStorageService _images;

    public PromptImportService(
        PromptRepository prompts,
        ImageStorageService images)
    {
        _prompts = prompts;
        _images = images;
    }

    public PromptImportPreview Inspect(string sourcePath)
    {
        ValidateSourcePath(sourcePath);

        var title = Path.GetFileNameWithoutExtension(sourcePath).Trim();
        if (title.Length == 0)
        {
            throw new InvalidDataException("파일 이름에서 제목을 가져올 수 없습니다.");
        }

        return new PromptImportPreview(
            sourcePath,
            title,
            FindMatchingImage(sourcePath));
    }

    public async Task<PromptItem> ImportAsync(
        string sourcePath,
        PromptCategory category,
        bool overwriteExisting = false,
        CancellationToken cancellationToken = default)
    {
        var preview = Inspect(sourcePath);
        var parsed = await ParseAsync(sourcePath, cancellationToken);
        var existing = await _prompts.GetByTitleAsync(
            category,
            preview.Title,
            cancellationToken);

        if (existing is not null && !overwriteExisting)
        {
            throw new InvalidOperationException(
                "같은 분류에 동일한 제목의 프롬프트가 이미 존재합니다.");
        }

        string? storedImagePath = null;
        var previousImagePath = existing?.ImagePath;
        PromptItem item;

        try
        {
            if (preview.ImageSourcePath is not null)
            {
                storedImagePath = await _images.ImportAsync(
                    preview.ImageSourcePath,
                    cancellationToken);
            }

            item = existing ?? new PromptItem
            {
                Category = category,
                Title = preview.Title
            };
            item.Title = preview.Title;
            item.PositivePrompt = parsed.PositivePrompt;
            item.NegativePrompt = parsed.NegativePrompt;
            item.Memo = parsed.Memo;
            item.ImagePath = storedImagePath;
            item.Tags.Clear();
            item.Tags.AddRange(parsed.Tags);

            if (existing is null)
            {
                await _prompts.CreateAsync(item, cancellationToken);
            }
            else
            {
                await _prompts.UpdateAsync(item, cancellationToken);
            }
        }
        catch
        {
            if (storedImagePath is not null)
            {
                try
                {
                    await _images.DeleteIfUnreferencedAsync(
                        storedImagePath,
                        cancellationToken);
                }
                catch
                {
                    // Preserve the original import failure.
                }
            }

            throw;
        }

        if (existing is not null &&
            previousImagePath is not null &&
            !string.Equals(
                previousImagePath,
                storedImagePath,
                StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await _images.DeleteIfUnreferencedAsync(
                    previousImagePath,
                    cancellationToken);
            }
            catch
            {
                // The prompt update succeeded; orphan cleanup can be retried later.
            }
        }

        return item;
    }

    private static async Task<ParsedPromptFile> ParseAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        string text;

        try
        {
            text = await File.ReadAllTextAsync(
                sourcePath,
                StrictUtf8,
                cancellationToken);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException(
                "텍스트 파일이 UTF-8 형식이 아닙니다.",
                ex);
        }

        var sections = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["Positive"] = [],
            ["Negative"] = [],
            ["Tags"] = [],
            ["Memo"] = []
        };

        string? currentSection = null;
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');

        foreach (var line in normalized.Split('\n'))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                var sectionName = trimmed[1..^1].Trim();
                if (!sections.ContainsKey(sectionName))
                {
                    throw new InvalidDataException(
                        $"지원하지 않는 섹션입니다: [{sectionName}]");
                }

                currentSection = sectionName;
                continue;
            }

            if (currentSection is null)
            {
                if (trimmed.Length == 0)
                {
                    continue;
                }

                throw new InvalidDataException(
                    "내용은 [Positive], [Negative], [Tags], [Memo] 섹션 안에 있어야 합니다.");
            }

            sections[currentSection].Add(line);
        }

        var positive = JoinBlock(sections["Positive"]);
        var negative = JoinBlock(sections["Negative"]);

        if (string.IsNullOrWhiteSpace(positive) &&
            string.IsNullOrWhiteSpace(negative))
        {
            throw new InvalidDataException(
                "Positive 또는 Negative 프롬프트 중 하나는 반드시 있어야 합니다.");
        }

        var tags = sections["Tags"]
            .SelectMany(line => line.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ParsedPromptFile(
            positive,
            negative,
            JoinBlock(sections["Memo"]),
            tags);
    }

    private static string JoinBlock(IEnumerable<string> lines) =>
        string.Join(Environment.NewLine, lines).Trim();

    private static string? FindMatchingImage(string sourcePath)
    {
        var directory = Path.GetDirectoryName(sourcePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        var baseName = Path.GetFileNameWithoutExtension(sourcePath);
        var files = Directory.EnumerateFiles(directory).ToArray();

        foreach (var extension in SupportedImageExtensions)
        {
            var match = files.FirstOrDefault(path =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    baseName,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    Path.GetExtension(path),
                    extension,
                    StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static void ValidateSourcePath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException("텍스트 파일을 찾을 수 없습니다.", sourcePath);
        }

        if (!string.Equals(
                Path.GetExtension(sourcePath),
                ".txt",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("TXT 파일만 가져올 수 있습니다.");
        }
    }

    private sealed record ParsedPromptFile(
        string PositivePrompt,
        string NegativePrompt,
        string Memo,
        IReadOnlyList<string> Tags);
}
