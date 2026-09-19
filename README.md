# DK Randomize AI Image Prompt Generator

Windows desktop application for storing reusable AI image-generation prompts and quickly combining them into editable Positive / Negative output.

## Main features

- Manage Character, Artist / Style, and Additional prompt groups.
- Store title, representative image, Positive prompt, Negative prompt, tags, and memo.
- Browse prompts in gallery or list view.
- Search prompt text and partially match tags; click a displayed tag to filter immediately.
- Sort prompts by updated time, created time, or title.
- Create, edit, duplicate, and delete prompt items.
- Choose Direct / Random / Disabled independently for each group.
- Select and order multiple prompts in Character, Artist / Style, and Additional.
- Remove items directly and reorder them with buttons or drag-and-drop.
- Choose a unique random item count independently for each category.
- Search title, tags, memo, Positive, and Negative together from the Mixer picker, with double-click add/remove and drag ordering.
- Randomize one group or all random-mode groups.
- Mixer selections show thumbnail, title, memo, and tags.
- Add a prompt directly from Prompt Library to the active Mixer session.
- Edit combined Positive / Negative output before copying.
- Copy Positive and Negative independently.
- Save final edited output plus selection order/modes to paged recent history, restore it later, or clear saved history.
- Follow the Windows system theme or force Light / Dark.
- Choose per-user LocalAppData storage or executable-folder portable storage.
- Remember the last main-window size, position, and maximized state.
- Check GitHub Releases for updates and install a newer win-x64 release from Settings.
- Back up and restore local data as a ZIP.

The application does **not** generate images directly. It prepares prompt text for use in other image-generation tools and services.

## Data storage

By default, application data is stored for the current Windows user:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Settings can switch the data root to the executable folder for portable use. Portable mode is indicated by a `portable.mode` marker next to the executable and takes effect on the next launch:

```text
<executable folder>\
├─ DKRandomizeAIImagePromptGenerator.exe
├─ portable.mode
├─ data\prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Changing storage mode does not automatically move data between roots. Use backup/restore when moving an existing library. Representative images are copied into application-managed storage; original image files are not modified.

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

Pushing a version tag such as `v1.0.0-rc.1` runs the Release workflow. The workflow verifies that the tag matches the WPF project version, rebuilds and smoke-tests the application, creates `DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64.zip`, and publishes it as a GitHub Release asset. The Settings update button checks those GitHub Releases and can download/install a newer compatible package.

For a local release-style verification and optional launch:

```powershell
.\scripts\verify-release.ps1 -Launch
```

Detailed requirements, architecture, UI direction, data model, task status, and current state are maintained under `docs/`.
