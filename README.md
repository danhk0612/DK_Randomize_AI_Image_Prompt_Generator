# DK Randomize AI Image Prompt Generator

Windows desktop application for storing reusable AI image-generation prompts and quickly combining them into editable Positive / Negative output.

## Main features

- Manage Character, Artist / Style, and Additional prompt groups.
- Store title, representative image, Positive prompt, Negative prompt, tags, and memo.
- Browse prompts in gallery or list view.
- Search prompt text and filter by tag.
- Create, edit, duplicate, and delete prompt items.
- Choose Fixed / Random / Disabled independently for each group.
- Randomize one group or all random-mode groups.
- Direct selection shows thumbnail, title, memo, and tags.
- Edit combined Positive / Negative output before copying.
- Copy Positive and Negative independently.
- Save final edited output to recent history and restore it later.
- Follow the Windows system theme or force Light / Dark.
- Back up and restore local data as a ZIP.

The application does **not** generate images directly. It prepares prompt text for use in other image-generation tools and services.

## Data storage

Application data is local to the current Windows user.

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Representative images are copied into application-managed storage. Original image files are not modified.

## Platform and development stack

Active release target:

- Windows desktop
- C# / .NET 10
- WPF / XAML
- SQLite (`Microsoft.Data.Sqlite`)
- self-contained win-x64 portable publish

The earlier WinUI implementation is retained in the repository as migration/reference code but is not the active release target.

## Development

Active development branch:

```text
feature/wpf-ui
```

Run the WPF application:

```powershell
dotnet run `
  --project ".\src\DKRandomizeAIImagePromptGenerator.Wpf\DKRandomizeAIImagePromptGenerator.Wpf.csproj" `
  -c Release `
  --property:Platform=x64
```

GitHub Actions validates build, automated tests, WPF routed mouse-wheel behavior, navigation/keyboard UI behavior, self-contained publish, and published application startup.

For a local release-style verification and optional launch:

```powershell
.\scripts\verify-release.ps1 -Launch
```

Detailed requirements, architecture, UI direction, data model, task status, and current state are maintained under `docs/`.
