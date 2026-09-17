# Current State

Updated: 2026-09-17

## Repository

The repository has been initialized and the first implementation branch is `feature/bootstrap`.

## Decisions fixed for V1

- Windows desktop application
- C# / .NET 10
- WinUI 3 using Windows App SDK 2.4 stable line
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

- Product requirements documented
- Architecture documented
- UI direction documented
- Initial data model documented
- Implementation milestones documented

## In progress

Milestone 0 bootstrap:

1. Create WinUI 3 project scaffold.
2. Create the NavigationView shell and placeholder pages.
3. Add the vector source for the app icon concept.
4. Add an initial CI build workflow after the project shape is stable enough to build on a Windows runner.

## Next functional milestone

After the shell builds, implement the prompt domain models and `CombinationService` first. The mixer logic should be testable before SQLite and before the full prompt-management UI are added.
