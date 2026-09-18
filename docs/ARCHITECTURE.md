# Architecture

## 1. Technology baseline

- Language: C#
- Runtime: .NET 10
- UI: WPF / XAML
- Pattern: MVVM-oriented separation
- Local database: SQLite
- Minimum OS target: Windows 10 1809+
- First distribution target: self-contained win-x64

The application is local-first. V1 has no server dependency and no direct AI API/image-generation dependency.

## 2. Project layout

```text
src/
├─ DKRandomizeAIImagePromptGenerator.Core/
│  └─ links the shared Models, Data, Services, and ViewModels
├─ DKRandomizeAIImagePromptGenerator.Wpf/
│  ├─ Converters/
│  ├─ Services/
│  ├─ Views/
│  ├─ App.xaml
│  ├─ App.xaml.cs
│  ├─ MainWindow.xaml
│  └─ MainWindow.xaml.cs
└─ DKRandomizeAIImagePromptGenerator/
   └─ legacy WinUI implementation retained as migration/reference source

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

### Core

The Core project reuses the existing UI-independent application code:

- Models
- SQLite repositories and database initialization
- combination/history/settings logic
- image storage and backup services
- ViewModels

UI-framework-specific code does not belong in Core.

### WPF UI

The WPF project owns:

- application startup
- navigation shell
- Mixer / Prompt Library / History / Settings views
- WPF file dialogs and clipboard integration
- image conversion for WPF
- mouse-wheel routing for outer page scroll areas
- responsive desktop layout behavior

### Legacy WinUI project

The original WinUI implementation remains in the repository during migration as a reference and recovery point. It is not the active release target.

## 4. Navigation

`MainWindow` hosts a left navigation pane and a content host.

Primary destinations:

- Mixer
- Prompt Library
- History
- Settings

The navigation pane switches to a compact icon-only layout when the window becomes narrow.

Prompt editing is shown as an in-app editor pane rather than a chain of modal dialogs.

## 5. Data flow

Typical mixer flow:

```text
Repositories -> MixerViewModel -> CombinationService -> editable output -> Clipboard
```

Typical prompt-edit flow:

```text
PromptLibraryView -> PromptLibraryViewModel -> PromptRepository
                                      └─────> ImageStorageService
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

`CombinationService` composes Positive and Negative output independently.

For each output:

1. Read Character text.
2. Read Artist text.
3. Read Additional text.
4. Remove empty/whitespace-only sections.
5. Join remaining sections with exactly one blank line.

The service must never alter prompt syntax.

## 8. Persistence

SQLite stores structured records. Images remain regular files.

Application data stays under:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
```

The WPF migration intentionally reuses the same database, images, backup, and settings locations as the original implementation.

## 9. Image storage

When the user chooses a representative image:

1. Read the source without modifying it.
2. Create an application-owned copy.
3. Store it under the app data image directory with a generated unique file name.
4. Persist only the relative application-owned path.
5. Remove orphaned application-owned copies when safe.

## 10. Scrolling strategy

The WPF UI uses normal WPF `ScrollViewer` controls.

For outer page/editor scroll areas, `WheelScrollService` handles WPF `PreviewMouseWheel` so wheel input is received before nested controls such as TextBox and ComboBox can consume it.

CI contains a routed-wheel smoke mode that creates a TextBox inside a ScrollViewer, raises a PreviewMouseWheel event against the TextBox, and verifies that the parent ScrollViewer offset changes.

## 11. Packaging strategy

V1 distribution is an unpackaged, self-contained `win-x64` WPF publish distributed as a ZIP archive.

No MSIX signing or Windows App SDK runtime is required by the active WPF release target.

## 12. Testing priorities

Highest-value automated tests:

- Fixed/Random/Disabled selection behavior
- Positive/Negative composition ordering
- empty-section handling
- random selection constrained to the requested category
- history captures final edited output
- repository CRUD and schema behavior
- backup/settings round trip
- WPF routed mouse-wheel smoke
- published WPF application startup smoke
