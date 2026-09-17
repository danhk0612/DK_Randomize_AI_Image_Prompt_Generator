# Current State

Updated: 2026-09-17

## Repository

Active development is on `feature/bootstrap`. The branch contains the complete V1 implementation candidate and is ready for Windows smoke testing before merge/release.

## Decisions fixed for V1

- Windows desktop application
- C# / .NET 10
- WinUI 3 using Windows App SDK 2.5.1
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
- Vector icon source based on three prompt cards and shuffle flow
- Windows executable ICO asset embedded through `ApplicationIcon`
- Accessibility names for icon-only Prompt Library controls

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

### Distribution preparation

- Self-contained unpackaged `win-x64` publish configuration validated in GitHub Actions
- Published output uploaded as a GitHub Actions artifact
- First-release distribution strategy documented as portable ZIP
- Manual Windows release checklist added
- First-release notes draft added; version/tag intentionally not chosen yet

## Verification status

The latest code validation on Windows completed successfully with:

1. Solution restore
2. Release build
3. 12 automated tests
4. Self-contained `win-x64` publish
5. Published artifact upload

A release artifact is approximately 96 MB before the user-facing release ZIP is finalized.

## Remaining before first release

1. Run `docs/RELEASE_CHECKLIST.md` against the exact published artifact on a real Windows desktop.
2. Decide whether additional user-facing error handling should be added for file/backup failures.
3. Fix any issues found by the smoke test.
4. Choose the release version/tag.
5. Merge the implementation branch and publish the first GitHub Release.
