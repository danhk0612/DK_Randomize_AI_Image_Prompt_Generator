# Current State

Updated: 2026-09-17

## Repository

Active development is on `feature/bootstrap`. The branch contains the complete V1 implementation candidate. Initial Windows launch has been confirmed on a real desktop, and the remaining work before release is final manual UI/feature smoke testing and release preparation.

## Decisions fixed for V1

- Windows desktop application
- C# / .NET 10
- WinUI 3 using Windows App SDK 2.4.0
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
- Responsive NavigationView pane sizing and page-width handling
- Responsive Mixer card layout: 3 columns, 2 columns, or 1 column depending on available width
- Responsive Prompt Library filters and editor width
- Responsive History and Settings layouts

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
- User-visible error dialogs for load, image, save, duplicate, and delete failures

### Mixer

- Fixed / Random / Disabled controls for all three categories
- Fixed item selection
- Per-category randomize while preserving other current selections
- Randomize all
- Representative image and title display
- Editable Positive / Negative result areas
- Independent copy actions
- Save final edited output to history
- User-visible error state when history saving fails

### History and settings

- Recent-history list and detail view
- Restore saved history back into the mixer
- System / Light / Dark theme setting persisted to `settings.json`
- ZIP backup of SQLite data, representative images, and settings
- ZIP restore flow with confirmation
- Runtime application-version display
- User-visible errors for theme persistence, backup, and restore failures
- Settings and backup round-trip tests

### Distribution preparation

- Self-contained unpackaged `win-x64` publish configuration validated in GitHub Actions
- Explicit publish inclusion of the application PRI resource to avoid runtime XAML loading failure
- Published EXE startup smoke test checks `startup-crash.log` and process survival
- Published output uploaded as a GitHub Actions artifact
- First-release distribution strategy documented as portable ZIP
- Manual Windows release checklist added
- First-release notes draft added; version/tag intentionally not chosen yet

## Verification status

The current automated Windows validation includes:

1. Solution restore
2. Release build
3. 12 automated tests
4. Self-contained `win-x64` publish
5. Published EXE startup smoke test
6. Published artifact upload

A real Windows desktop launch has also been confirmed after the PRI publish fix and Mixer initialization fix.

## Remaining before first release

1. Run the remaining manual checks in `docs/RELEASE_CHECKLIST.md` against the current published artifact.
2. Verify responsive layout while resizing the app and toggling the navigation pane.
3. Verify Prompt Library CRUD and representative image handling with real data.
4. Verify Mixer, History restore, theme, backup, and restore flows.
5. Fix any issues found by the smoke test.
6. Choose the release version/tag, merge the implementation branch, and publish the first GitHub Release.
