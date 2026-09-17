# Current State

Updated: 2026-09-17

## Repository

The repository is initialized and the first implementation branch is `feature/bootstrap`.

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

## Work completed

### Product and architecture

- Product requirements documented
- Architecture documented
- UI direction documented
- Initial data model documented
- Implementation milestones documented

### Milestone 0 — Bootstrap

- Added .NET 10 / WinUI 3 project scaffold
- Added Windows App SDK 2.5.1 dependency
- Added unpackaged self-contained development configuration
- Added `NavigationView` application shell
- Added initial Mixer, Prompt Library, History, and Settings pages
- Added shared page/card styling resources
- Added vector application-icon source based on three prompt cards plus shuffle flow
- Added Windows GitHub Actions restore/build workflow

## Current UI state

The application shell is intentionally functional but still data-free. The Mixer page establishes the target card layout and editable Positive/Negative output areas. Prompt Library, History, and Settings currently provide the first visual structure and empty states.

No SQLite or prompt CRUD logic is connected yet.

## Next functional milestone

Milestone 1 is next:

1. Add prompt domain models and enums.
2. Add selection/combination models.
3. Implement `CombinationService` with Fixed / Random / Disabled rules.
4. Add unit tests for selection and Positive/Negative composition behavior.

The mixer core should be fully testable before local persistence is introduced.
