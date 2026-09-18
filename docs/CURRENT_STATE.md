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
- Character → Artist → Additional composition order
- independent Positive and Negative composition
- empty-section handling
- automated combination tests

### Local persistence

- SQLite schema version 1
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
- Positive / Negative / memo / tags
- representative image import, preview, replace, remove, and orphan cleanup
- editor opens at the top
- responsive filter/editor layout

### Mixer

- Fixed / Random / Disabled for all categories
- direct selection with thumbnail, title, memo, and tags
- per-category randomize
- randomize all
- selected image/title/memo/tags display
- editable Positive / Negative results
- independent copy
- history save
- responsive 3-column / 2-column / 1-column card layout

### History and settings

- recent-history list/detail
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
3. 15 automated tests
4. self-contained win-x64 WPF publish
5. WPF routed mouse-wheel smoke
6. published EXE startup smoke
7. artifact upload

The routed-wheel smoke specifically verifies scrolling while the wheel target is a nested TextBox.

## Manual verification already confirmed

- WPF application launches on the user's Windows environment
- mouse-wheel scrolling works normally in the WPF build

## Remaining before first release

1. Complete the WPF functional-equivalence smoke pass against real data.
2. Check responsive layouts at narrow and wide window sizes.
3. Check keyboard/focus/accessibility behavior in WPF.
4. Validate backup/restore from the exact release artifact.
5. Update any remaining WinUI-specific documentation.
6. Choose release version/tag.
7. Merge only after the WPF build is approved for release.
