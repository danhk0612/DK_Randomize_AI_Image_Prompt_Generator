param(
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "DK_Randomize_AI_Image_Prompt_Generator.sln"
$project = Join-Path $root "src\DKRandomizeAIImagePromptGenerator.Wpf\DKRandomizeAIImagePromptGenerator.Wpf.csproj"
$tests = Join-Path $root "tests\DKRandomizeAIImagePromptGenerator.Tests\DKRandomizeAIImagePromptGenerator.Tests.csproj"
$launcherSourceDir = Join-Path $root "src\DKRandomizeAIImagePromptGenerator.Launcher"
$publishDir = Join-Path $root "artifacts\win-x64"
$appExe = Join-Path $publishDir "DKRandomizeAIImagePromptGenerator.App.exe"
$launcherExe = Join-Path $publishDir "DKRandomizeAIImagePromptGenerator.exe"

function Invoke-Smoke([string]$Argument, [string]$Name) {
    Write-Host "==> $Name"
    $process = Start-Process -FilePath $appExe -ArgumentList $Argument -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode)."
    }
}

function Build-NativeLauncher {
    $programFilesX86 = [Environment]::GetFolderPath("ProgramFilesX86")
    $vswhere = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        return $false
    }

    $vsPath = & $vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
    if ([string]::IsNullOrWhiteSpace($vsPath)) {
        return $false
    }

    $vcvars = Join-Path $vsPath "VC\Auxiliary\Build\vcvars64.bat"
    if (-not (Test-Path $vcvars)) {
        return $false
    }

    Write-Host "==> Build native .NET runtime launcher"
    Push-Location $launcherSourceDir
    try {
        $command = '"' + $vcvars + '" >nul && rc /nologo /fo Launcher.res Launcher.rc && cl /nologo /std:c++20 /O2 /EHsc /utf-8 /DUNICODE /D_UNICODE Launcher.cpp Launcher.res /link /SUBSYSTEM:WINDOWS /OUT:"' + $launcherExe + '" user32.lib shell32.lib advapi32.lib'
        & cmd.exe /d /s /c $command
        if ($LASTEXITCODE -ne 0) {
            throw "Native launcher build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Remove-Item "Launcher.res" -Force -ErrorAction SilentlyContinue
        Pop-Location
    }

    return $true
}

Push-Location $root
try {
    Write-Host "==> Restore"
    dotnet restore $solution -p:Platform=x64

    Write-Host "==> Release build"
    dotnet build $solution -c Release -p:Platform=x64 --no-restore

    Write-Host "==> Automated tests"
    dotnet test $tests -c Release -p:Platform=x64 --no-build --no-restore

    Write-Host "==> Publish framework-dependent single-file WPF app"
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish $project -c Release -r win-x64 -p:Platform=x64 --self-contained false --no-restore -o $publishDir

    if (-not (Test-Path $appExe)) {
        throw "Published application was not found: $appExe"
    }

    $publishedBeforeLauncher = @(Get-ChildItem -LiteralPath $publishDir -File)
    if ($publishedBeforeLauncher.Count -ne 1 -or $publishedBeforeLauncher[0].Name -ne "DKRandomizeAIImagePromptGenerator.App.exe") {
        $names = ($publishedBeforeLauncher | Select-Object -ExpandProperty Name) -join ", "
        throw "Framework-dependent single-file publish produced unexpected files: $names"
    }

    $launcherBuilt = Build-NativeLauncher
    if ($launcherBuilt) {
        if (-not (Test-Path $launcherExe)) {
            throw "Native launcher was not found after build: $launcherExe"
        }

        Write-Host "==> Native launcher runtime detection smoke"
        $runtimeCheck = Start-Process -FilePath $launcherExe -ArgumentList "--runtime-check" -Wait -PassThru
        if ($runtimeCheck.ExitCode -ne 0) {
            throw "Native launcher did not detect .NET 10 Desktop Runtime. Exit code: $($runtimeCheck.ExitCode)"
        }
    }
    else {
        Write-Warning "Visual C++ Build Tools were not found. Native launcher build is skipped locally; GitHub Actions validates the release launcher."
    }

    Invoke-Smoke "--scroll-smoke" "WPF mouse-wheel routing smoke"
    Invoke-Smoke "--ui-smoke" "WPF navigation/keyboard UI smoke"

    Write-Host ""
    Write-Host "Verification passed."
    Write-Host "Published application: $appExe"
    if ($launcherBuilt) {
        Write-Host "Native launcher: $launcherExe"
    }

    if ($Launch) {
        Write-Host "==> Launch"
        if ($launcherBuilt) {
            Start-Process -FilePath $launcherExe
        }
        else {
            Start-Process -FilePath $appExe
        }
    }
}
finally {
    Pop-Location
}
