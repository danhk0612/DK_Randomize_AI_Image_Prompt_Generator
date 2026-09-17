# Architecture

## 1. Technology baseline

- Language: C#
- Runtime: .NET 10
- UI: WinUI 3 / XAML
- Windows App SDK: 2.4.x stable line
- Pattern: MVVM
- Local database: SQLite
- Minimum OS: Windows 10 1809 (build 17763)

The application is local-first. V1 has no server dependency and no AI API dependency.

## 2. Project layout

```text
src/
└─ DKRandomizeAIImagePromptGenerator/
   ├─ Assets/
   ├─ Models/
   ├─ ViewModels/
   ├─ Views/
   ├─ Services/
   ├─ Data/
   ├─ App.xaml
   ├─ App.xaml.cs
   ├─ MainWindow.xaml
   └─ MainWindow.xaml.cs

tests/
└─ DKRandomizeAIImagePromptGenerator.Tests/

docs/
├─ REQUIREMENTS.md
├─ ARCHITECTURE.md
├─ UI_DESIGN.md
├─ DATA_MODEL.md
├─ TASKS.md
└─ CURRENT_STATE.md
```

## 3. Responsibilities

### Models

Pure application data structures and enums.

Initial models:

- `PromptItem`
- `PromptCategory`
- `PromptSelectionMode`
- `PromptCombination`
- `CombinationHistory`
- `Tag`
- `AppSettings`

### ViewModels

UI state and commands only. ViewModels must not directly access SQLite or the file system.

Initial ViewModels:

- `ShellViewModel`
- `MixerViewModel`
- `PromptLibraryViewModel`
- `HistoryViewModel`
- `SettingsViewModel`
- `PromptEditorViewModel`

### Services

Focused infrastructure/application services:

- `PromptRepository` — prompt CRUD and queries
- `HistoryRepository` — combination history persistence
- `DatabaseService` — database initialization and migrations
- `CombinationService` — deterministic composition rules and random selection
- `ImageStorageService` — representative-image import/remove/thumbnail handling
- `ClipboardService` — copy positive/negative result
- `BackupService` — backup and restore application-owned data
- `SettingsService` — local application settings

Do not add additional abstraction layers unless a concrete need appears.

## 4. Navigation

`MainWindow` hosts a WinUI `NavigationView` with a single content frame.

Primary destinations:

- Mixer
- Prompt Library
- History
- Settings

Prompt editing is opened as an in-app editor surface or secondary content pane rather than a chain of modal dialogs.

## 5. Data flow

Typical mixer flow:

```text
Repositories -> MixerViewModel -> CombinationService -> editable output -> Clipboard
```

Typical prompt-edit flow:

```text
PromptEditorViewModel -> PromptRepository
                     -> ImageStorageService
```

## 6. Randomization rules

Randomization is performed only by `CombinationService`.

For each category:

- Fixed: use the currently selected item.
- Random: choose one eligible item from that category.
- Disabled: return no item.

If a Random category has no eligible items, it contributes no prompt text. This is a normal empty state, not a fatal error.

V1 Additional selection returns zero or one item. The domain object keeps an item collection so multi-additional support can be added without replacing history/storage contracts.

## 7. Prompt composition rules

`CombinationService` composes positive and negative output independently.

For each output:

1. Read Character text.
2. Read Artist text.
3. Read Additional text.
4. Remove empty/whitespace-only sections.
5. Join remaining sections with exactly one blank line (`Environment.NewLine` twice).

The service must never alter prompt syntax.

## 8. Persistence

SQLite stores structured records. Images remain regular files.

Database versioning uses explicit migrations from the beginning. V1 starts at schema version 1.

Application data paths are supplied by one path service/value source rather than scattered literal paths.

## 9. Image storage

When the user chooses a representative image:

1. Read the source without modifying it.
2. Generate an application-owned display copy.
3. Store that copy under the app data image directory with a generated unique file name.
4. Persist only the relative application-owned path.
5. Remove orphaned application-owned copies when safe.

The initial implementation may preserve the source format; WebP thumbnail conversion can be added when image processing is implemented and tested.

## 10. Packaging strategy

Development starts with the standard WinUI 3 project structure and an unpackaged/debug-friendly workflow where useful. Final distribution strategy is decided after core features are stable.

Packaging concerns must stay separate from prompt-domain logic so MSIX or installer changes do not affect core behavior.

## 11. Testing priorities

Highest-value automated tests:

- Fixed/Random/Disabled selection behavior
- Positive/Negative composition ordering
- Empty-section handling
- Random selection constrained to the requested category
- History captures final edited output
- Repository CRUD and schema migration behavior

UI automation is not required for the first implementation milestone.
