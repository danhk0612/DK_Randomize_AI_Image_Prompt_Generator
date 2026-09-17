# First Release Notes — Draft

Version/tag is intentionally left open until the release candidate has passed the manual Windows smoke test.

## Highlights

- Windows desktop prompt library and random/fixed prompt mixer for AI image-generation workflows.
- Separate Character, Artist / Style, and Additional prompt libraries.
- Positive and Negative prompts stored and combined independently.
- Fixed, Random, and Disabled selection modes per category.
- Editable final output with independent clipboard copy actions.
- Local SQLite storage with tags, search, representative images, and notes.
- Gallery and list views for prompt management.
- Recent combination history preserving the final manually edited output.
- System, Light, and Dark themes.
- ZIP backup and restore for prompts, history, representative images, and settings.

## Distribution

- Windows `win-x64`
- Unpackaged self-contained portable build
- No separate .NET runtime installation required
- Distributed as a ZIP archive for the first release

## Current limitations

- The application prepares prompts only; it does not call an AI image-generation API.
- One Character, one Artist / Style, and one Additional prompt are used per V1 combination.
- Tag-constrained random pools are deferred.
- ARM64 project support exists, but the first release artifact is x64 only.
- No automatic updater is included in the first release.

## Before publishing

Complete `docs/RELEASE_CHECKLIST.md` using the exact release artifact, then choose the release version/tag and publish the GitHub Release.
