using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DKRandomizeAIImagePromptGenerator.Services;

public sealed record UpdateRelease(
    string Version,
    string TagName,
    string DisplayName,
    bool IsPrerelease,
    string HtmlUrl,
    string AssetName,
    string DownloadUrl,
    long AssetSize);

public sealed class GitHubUpdateService
{
    private const string UserAgent = "DK-Randomize-AI-Image-Prompt-Generator";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private readonly string _owner;
    private readonly string _repository;

    public GitHubUpdateService(string owner, string repository)
    {
        _owner = owner;
        _repository = repository;
    }

    public async Task<UpdateRelease?> CheckForUpdateAsync(
        string currentVersion,
        CancellationToken cancellationToken = default)
    {
        if (!SemanticVersion.TryParse(currentVersion, out var installedVersion))
        {
            throw new InvalidOperationException(
                $"현재 버전 '{currentVersion}'을(를) 해석할 수 없습니다.");
        }

        var url =
            $"https://api.github.com/repos/{_owner}/{_repository}/releases?per_page=20";

        await using var stream = await HttpClient.GetStreamAsync(url, cancellationToken);
        var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(
            stream,
            cancellationToken: cancellationToken) ?? [];

        var allowPrerelease = installedVersion.IsPrerelease;

        return releases
            .Where(release => !release.Draft && (allowPrerelease || !release.Prerelease))
            .Select(release => ToCandidate(release))
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .Where(candidate => candidate.ParsedVersion.CompareTo(installedVersion) > 0)
            .OrderByDescending(candidate => candidate.ParsedVersion)
            .Select(candidate => candidate.Release)
            .FirstOrDefault();
    }

    public async Task DownloadAndStartUpdateAsync(
        UpdateRelease release,
        string executablePath,
        int processId,
        CancellationToken cancellationToken = default)
    {
        var installDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("실행 파일 폴더를 확인할 수 없습니다.");

        EnsureInstallDirectoryWritable(installDirectory);

        var updateRoot = Path.Combine(
            Path.GetTempPath(),
            "DKPromptGeneratorUpdate",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(updateRoot);

        var archivePath = Path.Combine(updateRoot, release.AssetName);
        var scriptPath = Path.Combine(updateRoot, "apply-update.ps1");

        using (var response = await HttpClient.GetAsync(
                   release.DownloadUrl,
                   HttpCompletionOption.ResponseHeadersRead,
                   cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(
                archivePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);
            await source.CopyToAsync(destination, cancellationToken);
        }

        using (ZipFile.OpenRead(archivePath))
        {
            // Validate that the downloaded file is a readable ZIP before closing the app.
        }

        await File.WriteAllTextAsync(
            scriptPath,
            BuildUpdaterScript(),
            cancellationToken);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-ProcessId");
        startInfo.ArgumentList.Add(processId.ToString());
        startInfo.ArgumentList.Add("-Archive");
        startInfo.ArgumentList.Add(archivePath);
        startInfo.ArgumentList.Add("-InstallDirectory");
        startInfo.ArgumentList.Add(installDirectory);
        startInfo.ArgumentList.Add("-ExecutablePath");
        startInfo.ArgumentList.Add(executablePath);
        startInfo.ArgumentList.Add("-UpdateRoot");
        startInfo.ArgumentList.Add(updateRoot);

        if (Process.Start(startInfo) is null)
        {
            throw new InvalidOperationException("업데이트 적용 프로세스를 시작하지 못했습니다.");
        }
    }

    public static bool IsNewerVersion(string candidateVersion, string currentVersion) =>
        SemanticVersion.TryParse(candidateVersion, out var candidate) &&
        SemanticVersion.TryParse(currentVersion, out var current) &&
        candidate.CompareTo(current) > 0;

    private static void EnsureInstallDirectoryWritable(string installDirectory)
    {
        var probePath = Path.Combine(
            installDirectory,
            $".dk-update-write-test-{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(probePath, "update write test");
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException(
                "현재 실행 폴더에 업데이트 파일을 쓸 수 없습니다. " +
                "쓰기 가능한 폴더로 프로그램을 옮기거나 권한을 확인해 주세요.",
                ex);
        }
        finally
        {
            try
            {
                if (File.Exists(probePath))
                {
                    File.Delete(probePath);
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }

    private CandidateRelease? ToCandidate(GitHubReleaseDto dto)
    {
        if (!SemanticVersion.TryParse(dto.TagName, out var version))
        {
            return null;
        }

        var asset = dto.Assets
            .Where(asset =>
                asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                asset.Name.Contains("win-x64", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(asset =>
                asset.Name.Contains("WPF", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (asset is null || string.IsNullOrWhiteSpace(asset.BrowserDownloadUrl))
        {
            return null;
        }

        var release = new UpdateRelease(
            version.ToString(),
            dto.TagName,
            string.IsNullOrWhiteSpace(dto.Name) ? dto.TagName : dto.Name,
            dto.Prerelease,
            dto.HtmlUrl,
            asset.Name,
            asset.BrowserDownloadUrl,
            asset.Size);

        return new CandidateRelease(version, release);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(5)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static string BuildUpdaterScript() => """
param(
    [Parameter(Mandatory=$true)][int]$ProcessId,
    [Parameter(Mandatory=$true)][string]$Archive,
    [Parameter(Mandatory=$true)][string]$InstallDirectory,
    [Parameter(Mandatory=$true)][string]$ExecutablePath,
    [Parameter(Mandatory=$true)][string]$UpdateRoot
)

$ErrorActionPreference = 'Stop'
$logPath = Join-Path $env:TEMP 'DK-Prompt-Generator-update-error.log'

try {
    while (Get-Process -Id $ProcessId -ErrorAction SilentlyContinue) {
        Start-Sleep -Milliseconds 250
    }

    $stage = Join-Path $UpdateRoot 'stage'
    if (Test-Path -LiteralPath $stage) {
        Remove-Item -LiteralPath $stage -Recurse -Force
    }

    Expand-Archive -LiteralPath $Archive -DestinationPath $stage -Force

    $protectedNames = @(
        'portable.mode',
        'settings.json',
        'data',
        'images',
        'backups',
        'startup-crash.log'
    )

    foreach ($item in Get-ChildItem -LiteralPath $stage -Force) {
        if ($protectedNames -contains $item.Name) {
            continue
        }

        Copy-Item -LiteralPath $item.FullName -Destination $InstallDirectory -Recurse -Force
    }

    Start-Process -FilePath $ExecutablePath -WorkingDirectory $InstallDirectory
}
catch {
    $_ | Out-String | Set-Content -LiteralPath $logPath -Encoding UTF8
    exit 1
}
""";

    private sealed record CandidateRelease(
        SemanticVersion ParsedVersion,
        UpdateRelease Release);

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("draft")]
        public bool Draft { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubAssetDto> Assets { get; set; } = [];
    }

    private sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    private sealed class SemanticVersion : IComparable<SemanticVersion>
    {
        private SemanticVersion(
            int major,
            int minor,
            int patch,
            IReadOnlyList<string> prerelease)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Prerelease = prerelease;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
        public IReadOnlyList<string> Prerelease { get; }
        public bool IsPrerelease => Prerelease.Count > 0;

        public static bool TryParse(
            string? value,
            out SemanticVersion version)
        {
            version = null!;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var normalized = value.Trim();
            if (normalized.StartsWith('v') || normalized.StartsWith('V'))
            {
                normalized = normalized[1..];
            }

            var plusIndex = normalized.IndexOf('+');
            if (plusIndex >= 0)
            {
                normalized = normalized[..plusIndex];
            }

            var dashIndex = normalized.IndexOf('-');
            var core = dashIndex >= 0 ? normalized[..dashIndex] : normalized;
            var prereleaseText = dashIndex >= 0 ? normalized[(dashIndex + 1)..] : string.Empty;
            var parts = core.Split('.');
            var patch = 0;

            if (parts.Length < 2 ||
                parts.Length > 3 ||
                !int.TryParse(parts[0], out var major) ||
                !int.TryParse(parts[1], out var minor) ||
                (parts.Length == 3 && !int.TryParse(parts[2], out patch)))
            {
                return false;
            }

            var prerelease = string.IsNullOrWhiteSpace(prereleaseText)
                ? Array.Empty<string>()
                : prereleaseText.Split('.', StringSplitOptions.RemoveEmptyEntries);

            version = new SemanticVersion(
                major,
                minor,
                patch,
                prerelease);
            return true;
        }

        public int CompareTo(SemanticVersion? other)
        {
            if (other is null)
            {
                return 1;
            }

            var result = Major.CompareTo(other.Major);
            if (result != 0) return result;

            result = Minor.CompareTo(other.Minor);
            if (result != 0) return result;

            result = Patch.CompareTo(other.Patch);
            if (result != 0) return result;

            if (!IsPrerelease && !other.IsPrerelease) return 0;
            if (!IsPrerelease) return 1;
            if (!other.IsPrerelease) return -1;

            var count = Math.Max(Prerelease.Count, other.Prerelease.Count);
            for (var index = 0; index < count; index++)
            {
                if (index >= Prerelease.Count) return -1;
                if (index >= other.Prerelease.Count) return 1;

                var left = Prerelease[index];
                var right = other.Prerelease[index];

                var leftNumeric = int.TryParse(left, out var leftNumber);
                var rightNumeric = int.TryParse(right, out var rightNumber);

                if (leftNumeric && rightNumeric)
                {
                    result = leftNumber.CompareTo(rightNumber);
                }
                else if (leftNumeric)
                {
                    result = -1;
                }
                else if (rightNumeric)
                {
                    result = 1;
                }
                else
                {
                    result = string.Compare(
                        left,
                        right,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (result != 0) return result;
            }

            return 0;
        }

        public override string ToString()
        {
            var core = $"{Major}.{Minor}.{Patch}";
            return IsPrerelease
                ? $"{core}-{string.Join('.', Prerelease)}"
                : core;
        }
    }
}
