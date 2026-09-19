# Distribution

## Stable distribution

The stable Windows release is distributed as a compact portable ZIP for `win-x64`.

The ZIP contains exactly two application files:

```text
DKRandomizeAIImagePromptGenerator.exe
DKRandomizeAIImagePromptGenerator.App.exe
```

- `DKRandomizeAIImagePromptGenerator.exe`: native Windows launcher
- `DKRandomizeAIImagePromptGenerator.App.exe`: framework-dependent single-file WPF application

The .NET runtime is intentionally not bundled. The launcher checks for Microsoft .NET 10 Desktop Runtime x64 before starting the WPF application. If the runtime is missing, it offers to open Microsoft's official .NET 10 download page.

## Why this format

- Very small release ZIP compared with bundling the full .NET runtime.
- No installer is required.
- The user can extract the two files and run the launcher directly.
- Application updates only need to replace the application files.
- User data is independent from the program binaries.

## User data

The default data root is:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
```

Settings can switch to executable-folder portable storage. User data is not part of the release ZIP.

## CI and release validation

The build/release workflows validate:

1. Restore
2. Release build
3. Automated tests
4. Framework-dependent single-file WPF publish
5. Native launcher compilation
6. Exact two-file package layout
7. .NET Desktop Runtime detection by the launcher
8. WPF mouse-wheel smoke
9. WPF navigation/keyboard UI smoke
10. Launcher-to-WPF startup smoke
11. ZIP creation and GitHub Release publishing

The release asset is:

```text
DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64.zip
```

## Updating

The application checks GitHub Releases from Settings. When the user approves an update, the new ZIP is downloaded, application files are replaced after shutdown, and the launcher starts the updated version.

Portable and LocalAppData user data are excluded from application-file replacement.
