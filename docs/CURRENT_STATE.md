# Current State

Updated: 2026-09-17

## Repository

The repository is initialized and active development is on `feature/bootstrap`.

## Decisions fixed for V1

- Windows desktop application
- C# / .NET 10
- WinUI 3 using Windows App SDK 2.5 stable line
- Minimum OS target: Windows 10 1809+
- Local-first architecture
- SQLite for structured data
- Application-owned representative image copies
- Three prompt categories: Character, Artist/Style, Additional
- Selection modes: Fixed, Random, Disabled
- Separate Positive and Negative composition
- Generated outputs remain manually editable before copy
- Recent-history records preserve final edited text
- No direct AI API/image-generation feature in V1

## Implemented

### Application shell and design

- WinUI 3 / .NET 10 application scaffold
- Fluent-style `NavigationView` shell
- Mixer, Prompt Library, History, and Settings pages
- Shared card/title styles and system/light/dark themes
- Vector icon design source based on three prompt cards and shuffle flow
- GitHub Actions restore/build/test workflow

### Prompt domain and combination core

- Prompt category and selection-mode models
- Fixed / Random / Disabled selection rules
- Character → Artist → Additional composition order
- Independent Positive and Negative output composition
- Empty-section handling without duplicate blank lines
- Unit tests for core combination behavior

### Local persistence

- SQLite schema version 1 and migration initialization
- Prompt CRUD repository
- Tag persistence and filtering
- Title / Positive / Negative search
- Combination-history persistence with final edited text snapshots
- Prompt deletion preserves history text snapshots
- Repository persistence tests

### Prompt Library

- Character / Artist / Additional category switching
- Gallery and list display modes
- Search and tag filter
- Create / edit / duplicate / delete
- Positive / Negative / memo / tags fields
- Representative image import, preview, replace, remove, and orphan cleanup
- Application-owned image copies under local app data

### Mixer

- Fixed / Random / Disabled controls for all three categories
- Fixed item selection
- Per-category randomize while preserving other current selections
- Randomize all
- Representative image and title display
- Editable Positive / Negative result areas
- Independent copy actions
- Save final edited output to history

### History and settings

- Recent-history list and detail view
- Restore saved history back into the mixer
- System / Light / Dark theme setting persisted to `settings.json`
- ZIP backup of SQLite data, representative images, and settings
- ZIP restore flow with confirmation
- Runtime application-version display
- Settings and backup round-trip tests

## Verification status

GitHub Actions has repeatedly validated the WinUI solution on Windows runners with .NET 10. The branch is currently being revalidated after the latest Settings and Prompt Library UI changes.

## Next milestone

Milestone 6 — polish and distribution:

1. Produce final Windows application icon assets from the approved vector concept.
2. Run keyboard/accessibility and empty/error-state passes.
3. Validate a publish/release build.
4. Choose the final packaging/installer strategy.
5. Prepare the first distributable release.
