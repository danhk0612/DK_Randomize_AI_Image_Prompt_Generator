# First Release Notes — Draft

Current release-candidate version: `1.0.0-rc.1`. Final `v1.0.0` remains gated on the release-candidate validation checklist.

## Highlights

- Windows desktop prompt library and prompt mixer for AI image-generation workflows.
- WPF UI with stable mouse-wheel behavior.
- Separate Character, Artist / Style, and Additional prompt libraries.
- Ordered multi-select in all three categories.
- Direct / Random / Disabled radio modes per category.
- Unique Random N-item selection with a configurable count per category.
- Integrated category picker searching title, tags, memo, Positive, and Negative together.
- Prompt Library **Add to Mixer** action with duplicate prevention.
- Shared Mixer session preserves selection state and manual output edits during normal navigation.
- Positive and Negative prompts stored and composed independently.
- Non-empty prompt fragments are joined with one line break.
- Editable final output with independent clipboard copy actions.
- SQLite storage with tags, search, representative images, and notes.
- Gallery and list views for prompt management, with updated/created/title sorting.
- Partial tag filtering plus clickable tag shortcuts.
- Per-item removal and drag ordering in Mixer; double-click add/remove and drag ordering in the picker.
- Paged recent history: 20 records per page, multi-selection summaries, source thumbnails, exact Mixer restore, and clear-all history.
- System, Light, and Dark themes with theme-aware buttons, selections, ComboBoxes, dialogs, and disabled/random states.
- Choose between per-user LocalAppData storage and executable-folder portable storage.
- Main window remembers its previous size, position, and maximized state.
- Settings includes a GitHub Releases update checker and user-confirmed automatic download/install/restart flow.
- Tag-driven GitHub Release workflow publishes the self-contained win-x64 ZIP used by the updater.
- ZIP backup/restore for prompts, schema-v2 history, representative images, and settings.
- Responsive desktop layouts and compact navigation at narrow widths.
- Existing LocalAppData and schema-v1 history are migrated forward automatically.

## Reliability

- 26 automated tests in the expanded candidate pipeline.
- Explicit schema-v1 → schema-v2 migration coverage.
- Multi-select ordering, unique random count, paging, exact History restore, and schema-v2 backup/restore regression coverage.
- CI mouse-wheel routing smoke with a nested TextBox target.
- Expanded WPF UI smoke covers all four primary views, multi-select mode controls, random count, integrated picker search, shared Mixer session, Prompt Library add-to-Mixer, History paging/thumbnails, responsive navigation, accessibility metadata, and keyboard focus traversal.
- Published executable startup smoke with startup-crash logging.
- Invalid backup archives are rejected before destructive restore.
- Corrupted settings JSON is preserved and automatically reset to safe defaults.

## Distribution

- Windows `win-x64`
- WPF / .NET 10
- Unpackaged self-contained portable build
- No separate .NET runtime installation required
- Distributed as a ZIP archive for the first release

## Current limitations

- The application prepares prompts only; it does not call an AI image-generation API.
- Tag-constrained random pools are deferred.
- Mixer session state is preserved while the app runs but is not restored across application restarts.
- History thumbnails use the current representative image when the source prompt still exists; deleted prompts retain title snapshots but no archived image copy.
- First release is x64 only.
- Updates require access to GitHub Releases; offline environments can continue using manual ZIP replacement.

## Before publishing

Complete the remaining manual items in `docs/RELEASE_CHECKLIST.md` using the exact expanded release-candidate artifact. Then choose the version/tag and publish the GitHub Release.
