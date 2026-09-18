# Distribution

## First release strategy

The first release uses an **unpackaged, self-contained WPF win-x64 publish** distributed as a ZIP archive.

Reasons:

- No installer or signing certificate is required.
- The user can extract and run the application directly.
- The .NET runtime is included by the self-contained publish.
- WPF does not require the Windows App SDK runtime used by the legacy WinUI implementation.
- User data remains under LocalAppData and is independent of the executable folder.

## CI validation

The build workflow performs:

1. Restore
2. Release build
3. Automated tests
4. self-contained WPF win-x64 publish
5. WPF mouse-wheel routing smoke
6. published application startup smoke
7. artifact upload

The artifact name is:

```text
DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64
```

## Release artifact

The published executable is:

```text
DKRandomizeAIImagePromptGenerator.exe
```

The first release remains portable ZIP only.

## Options after V1

Not part of the first release:

- installer EXE
- MSIX
- Store packaging
- automatic update service
- ARM64 WPF distribution

These can be added later without changing the existing LocalAppData storage contract.
