# Current State

Updated: 2026-09-23

## Repository

The stable and active development baseline is `main`. Feature work branches from `main` and returns through verified pull requests.

The current tree contains only the active WPF application, Core library, native launcher, tests, and shared icon resources. The older WinUI implementation remains available through Git history / the historical `feature/bootstrap` branch rather than being duplicated in the active source tree.

## Decisions fixed for V1

- Windows desktop application
- C# / .NET 10
- WPF / XAML active UI
- Local-first architecture
- SQLite for structured data
- Existing local data path preserved across the UI migration
- Application-owned representative image copies
- Three prompt categories: Character, Artist/Style, Additional
- Selection modes: Fixed, Random, Disabled
- Separate Positive and Negative composition
- Generated outputs remain manually editable before copy
- Recent-history records preserve final edited text
- No direct AI API/image-generation feature in V1
- First release target: compact win-x64 portable ZIP with a native runtime-check launcher and framework-dependent single-file WPF app

## Implemented

### WPF application shell

- WPF / .NET 10 application
- framework-dependent single-file publish for the actual WPF application
- native Windows launcher detects .NET 10 Desktop Runtime x64 before starting the WPF app
- missing runtime prompt can open Microsoft's official .NET 10 download page
- Mixer, Prompt Library, History, and Settings views
- left navigation shell with compact mode for narrow windows
- System / Light / Dark theme support
- existing application icon reused
- responsive page layouts
- WPF PreviewMouseWheel routing for page/editor scroll areas

### Prompt domain and combination core

- Prompt category and selection-mode models
- Fixed / Random / Disabled rules
- ordered multi-select in all three categories
- unique random N-item selection per category
- single-newline prompt composition
- Character → Artist → Additional composition order
- independent Positive and Negative composition
- empty-section handling
- automated combination tests

### Local persistence

- SQLite schema version 2 with automatic v1 history migration
- prompt CRUD
- tag persistence and filtering
- title/prompt search
- combination-history persistence
- prompt deletion preserves history text
- existing LocalAppData paths preserved

### Prompt Library

- category switching
- gallery and list modes
- search plus partial tag filtering
- clickable tags that immediately apply the tag filter
- sort by updated time, created time, or title
- create / edit / duplicate / delete
- add an existing prompt directly to the shared Mixer session
- prompt create/update/delete changes synchronize with the active Mixer session
- Positive / Negative / memo / tags
- representative image import, preview, replace, remove, and orphan cleanup
- editor opens at the top
- responsive filter/editor layout
- bulk UTF-8 TXT import from a folder or multi-file selection
- file-name-derived titles and current-category assignment
- Positive or Negative required for imported files; tags/memo/image optional
- same-basename representative image auto-import
- per-file progress, failure reason, and final batch summary
- same-category duplicate-title protection without overwrite
- bulk-import automated coverage for positive-only, negative-only, required prompt validation, duplicate rejection, cross-category titles, and image matching

### Mixer

- Direct / Random / Disabled radio modes for all categories
- multi-select result lists with per-item remove, ordering controls, and drag reorder
- per-category random count
- integrated category search dialog across title/tags/memo/Positive/Negative
- picker supports double-click add/remove and drag reorder
- direct selection with thumbnail, title, memo, and tags
- per-category randomize
- randomize all
- selected image/title/memo/tags display
- editable Positive / Negative results
- independent copy
- history save
- Mixer state and manually edited output survive normal page navigation
- responsive 3-column / 2-column / 1-column card layout

### History and settings

- top full-width recent-history list with 20-item paging
- multi-selection summaries and current representative-image thumbnails
- exact selection mode/order/random-count restore into Mixer
- restore history to Mixer
- delete one history record or clear all history with confirmation
- System / Light / Dark theme persistence
- selectable data root: per-user LocalAppData or executable-folder portable mode
- portable mode is persisted by an executable-side `portable.mode` marker and takes effect on the next launch
- changing the data root does not automatically move existing data; backup/restore can be used when moving data
- main-window normal bounds and maximized state persist in `settings.json`
- invalid/off-screen saved placement falls back to a visible startup position
- Settings can check GitHub Releases for a newer compatible `win-x64` ZIP
- updates are user-initiated: download, close, replace application files, and restart automatically
- updater preserves executable-folder portable data roots such as `portable.mode`, `settings.json`, `data`, `images`, and `backups`
- stable builds ignore prerelease GitHub releases; prerelease builds can move to newer prerelease or stable releases
- `.github/workflows/release.yml` runs from a versioned `release/v...` branch, validates the build, creates the matching tag, and publishes the update ZIP to GitHub Releases
- ZIP backup/restore
- version display
- responsive History and Settings layouts

## Automated verification

The current WPF pipeline validates:

1. Solution restore
2. Release build
3. 31 automated tests
4. framework-dependent single-file win-x64 WPF publish
5. WPF routed mouse-wheel smoke
6. native launcher/runtime detection and published-app startup smoke
7. WPF navigation / responsive shell / keyboard-focus UI smoke
8. artifact upload

The routed-wheel smoke specifically verifies scrolling while the wheel target is a nested TextBox. The WPF UI smoke seeds temporary prompt/history data, opens the actual MainWindow, switches all four views, verifies responsive navigation, checks navigation accessibility metadata, and confirms basic keyboard focus traversal.

## Manual verification status

The expanded WPF build has now been exercised on the user's Windows environment with real prompt data. Core navigation, Mixer interaction, picker behavior, Prompt Library changes, History UI, responsive layout, and the Dark-theme visual fixes have been confirmed working in normal use.

The latest visual pass fixed theme-aware page/section titles, themed ComboBoxes, picker-window background, random-result surfaces, selection colors, button hierarchy, and overflow/alignment issues.

`v1.0.0` has been published successfully. Remaining unchecked items in the release checklist are optional/manual coverage items rather than blockers for the published stable release.

## Release candidate status

- `v1.0.0-rc.1` remains published as the earlier self-contained GitHub prerelease.
- `v1.0.0-rc.2` is published as the current compact GitHub prerelease.
- RC2 release branch: `release/v1.0.0-rc.2`
- RC2 release commit/tag: `f268bee17fa759aa403237be29d343d6f38cd655`
- RC2 distribution contains exactly two application files: native launcher + framework-dependent single-file WPF app.
- RC2 release ZIP is approximately 1.52 MB, compared with the earlier runtime-bundled package of roughly 66 MB.
- RC2 Release workflow passed Restore, Build, 26 tests, single-file Publish, native launcher build/runtime detection, compact package validation, routed-wheel smoke, UI smoke, launcher-to-app startup smoke, ZIP creation, release publishing, and post-publish verification.

## v1.0.0 release status

- `v1.0.0` is published as the first stable GitHub Release.
- Release branch: `release/v1.0.0`
- Release commit/tag: `2b7f0c22a1a2fa347b07c9054db913af7f0b5dd8`
- Final Build workflow passed Restore, Build, 26 tests, framework-dependent single-file Publish, native launcher compilation/runtime detection, exact two-file package validation, WPF smoke tests, launcher-to-app startup, and artifact upload.
- Final Release workflow repeated the complete validation and successfully created the tag, stable GitHub Release, and update ZIP.
- Stable release asset: `DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64.zip` (approximately 1.52 MB).
- The user manually confirmed the compact two-file RC2 folder and normal application startup before stable promotion.
- Optional post-release validation remains for the missing-runtime dialog/download-link path on a Windows machine without .NET 10 Desktop Runtime x64.
