# First Release Notes — Draft

Version/tag is intentionally left open until the release candidate has passed the final manual Windows smoke test.

## Highlights

- Windows desktop prompt library and random/fixed prompt mixer for AI image-generation workflows.
- Active UI rebuilt in WPF for stable Windows mouse-wheel behavior.
- Separate Character, Artist / Style, and Additional prompt libraries.
- Positive and Negative prompts stored and combined independently.
- Fixed, Random, and Disabled selection modes per category.
- Direct-selection UI with thumbnail, title, memo, and tags.
- Editable final output with independent clipboard copy actions.
- Local SQLite storage with tags, search, representative images, and notes.
- Gallery and list views for prompt management.
- Recent combination history preserving the final manually edited output.
- System, Light, and Dark themes.
- ZIP backup and restore for prompts, history, representative images, and settings.
- Responsive desktop layouts and compact navigation at narrow widths.
- Existing LocalAppData from the earlier implementation is reused.

## Reliability

- 15 automated tests.
- CI mouse-wheel routing smoke with a nested TextBox target.
- CI WPF UI smoke covering all four primary views, responsive navigation, accessibility metadata, and keyboard focus traversal.
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
- One Character, one Artist / Style, and one Additional prompt are used per V1 combination.
- Tag-constrained random pools are deferred.
- First release is x64 only.
- No automatic updater is included in the first release.

## Before publishing

Complete the remaining manual items in `docs/RELEASE_CHECKLIST.md` using the exact release candidate artifact. Then choose the version/tag and publish the GitHub Release.
