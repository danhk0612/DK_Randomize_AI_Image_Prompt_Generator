# Current State

Updated: 2026-09-18

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
- First release target: self-contained win-x64 portable ZIP

## Implemented

### WPF application shell

- WPF / .NET 10 application
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
- search and tag filter
- create / edit / duplicate / delete
- add an existing prompt directly to the shared Mixer session
- prompt create/update/delete changes synchronize with the active Mixer session
- Positive / Negative / memo / tags
- representative image import, preview, replace, remove, and orphan cleanup
- editor opens at the top
- responsive filter/editor layout

### Mixer

- Direct / Random / Disabled radio modes for all categories
- multi-select result lists with remove and ordering controls
- per-category random count
- integrated category search dialog across title/tags/memo/Positive/Negative
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
- delete history
- System / Light / Dark theme persistence
- ZIP backup/restore
- version display
- responsive History and Settings layouts

## Automated verification

The current WPF pipeline validates:

1. Solution restore
2. Release build
3. 23 automated tests
4. self-contained win-x64 WPF publish
5. WPF routed mouse-wheel smoke
6. published EXE startup smoke
7. WPF navigation / responsive shell / keyboard-focus UI smoke
8. artifact upload

The routed-wheel smoke specifically verifies scrolling while the wheel target is a nested TextBox. The WPF UI smoke seeds temporary prompt/history data, opens the actual MainWindow, switches all four views, verifies responsive navigation, checks navigation accessibility metadata, and confirms basic keyboard focus traversal.

## Manual verification already confirmed

- WPF application launches on the user's Windows environment
- mouse-wheel scrolling works normally in the WPF build
- `scripts/verify-release.ps1 -Launch` completed successfully on the user's Windows environment
- local Release build succeeded
- all 15 automated tests passed locally
- local self-contained `win-x64` publish succeeded
- local WPF mouse-wheel and navigation/keyboard UI smoke modes passed
- the published WPF executable launched successfully

## Remaining before first release

1. Run the remaining visual/manual checks in `docs/RELEASE_CHECKLIST.md` against real user data.
2. Verify visible keyboard focus styling in Light and Dark themes.
3. Validate backup/restore once from the exact release candidate artifact.
4. Choose release version/tag.
5. Merge only after the WPF build is approved for release.
