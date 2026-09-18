param(
    [switch]$Launch
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "DK_Randomize_AI_Image_Prompt_Generator.sln"
$project = Join-Path $root "src\DKRandomizeAIImagePromptGenerator.Wpf\DKRandomizeAIImagePromptGenerator.Wpf.csproj"
$tests = Join-Path $root "tests\DKRandomizeAIImagePromptGenerator.Tests\DKRandomizeAIImagePromptGenerator.Tests.csproj"
$publishDir = Join-Path $root "artifacts\win-x64"
$exe = Join-Path $publishDir "DKRandomizeAIImagePromptGenerator.exe"

function Invoke-Smoke([string]$Argument, [string]$Name) {
    Write-Host "==> $Name"
    $process = Start-Process -FilePath $exe -ArgumentList $Argument -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "$Name failed with exit code $($process.ExitCode)."
    }
}

Push-Location $root
try {
    Write-Host "==> Restore"
    dotnet restore $solution -p:Platform=x64

    Write-Host "==> Release build"
    dotnet build $solution -c Release -p:Platform=x64 --no-restore

    Write-Host "==> Automated tests"
    dotnet test $tests -c Release -p:Platform=x64 --no-build --no-restore

    Write-Host "==> Publish WPF win-x64"
    Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
    dotnet publish $project -c Release -r win-x64 -p:Platform=x64 --self-contained true --no-restore -o $publishDir

    if (-not (Test-Path $exe)) {
        throw "Published executable was not found: $exe"
    }

    Invoke-Smoke "--scroll-smoke" "WPF mouse-wheel routing smoke"
    Invoke-Smoke "--ui-smoke" "WPF navigation/keyboard UI smoke"

    Write-Host ""
    Write-Host "Verification passed."
    Write-Host "Published application: $exe"

    if ($Launch) {
        Write-Host "==> Launch"
        Start-Process -FilePath $exe
    }
}
finally {
    Pop-Location
}
