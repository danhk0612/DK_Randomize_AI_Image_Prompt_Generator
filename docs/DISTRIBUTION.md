# Distribution

## First release strategy

The first distributable build will use an **unpackaged, self-contained `win-x64` publish** and be distributed as a ZIP archive.

Reasons for V1:

- No MSIX signing certificate is required.
- The user can extract and run the application directly.
- The .NET runtime and Windows App SDK runtime dependencies are included by the self-contained publish configuration.
- The release structure matches the current unpackaged application architecture and keeps first-release validation simple.

## CI validation

The main build workflow performs:

1. Restore
2. Release build
3. Automated tests
4. `dotnet publish` for `win-x64`
5. Upload of the published directory as a GitHub Actions artifact

The publish output is therefore validated on every branch build before the first GitHub Release is prepared.

## Installer / package options after V1

The following are deliberately not part of the first release:

- MSIX packaging and signing
- Installer EXE generation
- Microsoft Store packaging
- Automatic update service

An installer can be added later without changing the application's local data location because user data already lives under `%LOCALAPPDATA%` rather than beside the executable.

## Architecture targets

The project retains `x64` and `ARM64` targets, but the first release artifact is `win-x64`. ARM64 distribution can be enabled after the x64 release path is validated.
