# Current State

Updated: 2026-09-19

## Repository

Active development is now on `feature/wpf-ui`.

The original WinUI implementation remains on `feature/bootstrap` and in the legacy source folder for reference/recovery, but it is no longer the active release target.

The WPF migration was made after repeated WinUI mouse-wheel routing problems. The WPF build has been confirmed locally to scroll correctly by the user.

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
3. 26 automated tests
4. self-contained win-x64 WPF publish
5. WPF routed mouse-wheel smoke
6. published EXE startup smoke
7. WPF navigation / responsive shell / keyboard-focus UI smoke
8. artifact upload

The routed-wheel smoke specifically verifies scrolling while the wheel target is a nested TextBox. The WPF UI smoke seeds temporary prompt/history data, opens the actual MainWindow, switches all four views, verifies responsive navigation, checks navigation accessibility metadata, and confirms basic keyboard focus traversal.

## Manual verification status

The expanded WPF build has now been exercised on the user's Windows environment with real prompt data. Core navigation, Mixer interaction, picker behavior, Prompt Library changes, History UI, responsive layout, and the Dark-theme visual fixes have been confirmed working in normal use.

The latest visual pass fixed theme-aware page/section titles, themed ComboBoxes, picker-window background, random-result surfaces, selection colors, button hierarchy, and overflow/alignment issues.

The final release gate still requires an exact release-candidate pass for keyboard focus visibility and backup/restore, plus the remaining checklist items that are specifically release-artifact checks.

## Release candidate status

- `v1.0.0-rc.1` is published as the earlier self-contained GitHub prerelease.
- The current source version is `1.0.0-rc.2`.
- RC2 changes distribution to two files: native launcher + framework-dependent single-file WPF app.
- RC2 must pass the compact-package CI checks before `release/v1.0.0-rc.2` is created and published.

## Remaining before final v1.0.0

1. Publish and manually verify `v1.0.0-rc.2`, including launcher behavior with .NET 10 Desktop Runtime present and absent.
2. Verify the real in-app update path from the published `v1.0.0-rc.1` build to `v1.0.0-rc.2`.
3. Confirm portable and LocalAppData user data survive the two-file update.
4. If no RC2 defects are found, change the project version from `1.0.0-rc.2` to `1.0.0`.
5. Create `release/v1.0.0` from the approved final commit and trigger the Release workflow.
