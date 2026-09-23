# Architecture

## 1. Technology baseline

- Language: C#
- Runtime: .NET 10
- UI: WPF / XAML
- Pattern: MVVM-oriented separation
- Local database: SQLite
- Minimum OS target: Windows 10 1809+
- Distribution target: compact win-x64 ZIP with native launcher + framework-dependent single-file WPF app

The application is local-first. V1 has no server dependency and no direct AI API/image-generation dependency.

## 2. Project layout

```text
src/
├─ DKRandomizeAIImagePromptGenerator.Core/
│  ├─ Models/
│  ├─ Data/
│  ├─ Services/
│  └─ ViewModels/
├─ DKRandomizeAIImagePromptGenerator.Wpf/
│  ├─ Converters/
│  ├─ Services/
│  ├─ Views/
│  ├─ App.xaml
│  ├─ App.xaml.cs
│  ├─ MainWindow.xaml
│  └─ MainWindow.xaml.cs
├─ DKRandomizeAIImagePromptGenerator.Launcher/
│  └─ native .NET Desktop Runtime check/startup launcher
└─ DKRandomizeAIImagePromptGenerator/
   └─ shared application icon resources

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

The Core project owns all UI-independent application code:

- Models
- SQLite repositories and database initialization
- combination/history/settings logic
- image storage, backup, and update services
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
PromptRepository
      ↓
MixerViewModel (ordered selections + modes + random counts)
      ↓
CombinationService
      ↓
editable Positive / Negative output
      ↓
Clipboard / History
```

The WPF shell retains one MixerView instance for the application session so selections and manually edited output survive normal page navigation.

Typical prompt-edit flow:

```text
PromptLibraryView -> PromptLibraryViewModel -> PromptRepository
          │                           └─────> ImageStorageService
          └──── add/update/delete ──────────> shared Mixer session
```

Typical bulk-import flow:

```text
PromptLibraryView
      ↓
PromptImportDialog (folder/files + per-file progress)
      ↓
PromptImportService
      ├─ strict UTF-8 section parsing
      ├─ same-category title duplicate check
      ├─ same-basename image discovery
      ├─ ImageStorageService
      └─ PromptRepository
```

Each file is its own import unit. A failure does not roll back earlier successful files or prevent later files from running. If an image copy succeeds but prompt persistence fails, the new unreferenced application-owned image is cleaned up.

## 6. Randomization rules

Randomization is performed only by `CombinationService`.

For each category:

- Direct: use the ordered manually selected items.
- Random: choose the requested number of unique eligible items.
- Disabled: return no items.

Random count is capped at the number of available candidates. If a Random category has no eligible items, it contributes no prompt text. This is a normal empty state, not a fatal error.

Character, Artist / Style, and Additional use the same multi-item selection contract.

## 7. Prompt composition rules

`CombinationService` composes Positive and Negative output independently.

For each output:

1. Read Character items in selection order.
2. Read Artist / Style items in selection order.
3. Read Additional items in selection order.
4. Remove empty/whitespace-only fragments.
5. Join remaining fragments with exactly one line break.

The service must never alter prompt syntax.

## 8. Persistence

SQLite stores structured records. Images remain regular files.

Schema version 2 adds generic `CombinationHistoryItems` and `CombinationHistoryCategoryState` tables so every category can store multiple ordered source prompts plus mode/random-count state. Existing schema-v1 history is migrated forward automatically while the original V1 tables remain for compatibility.

By default, application data stays under:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
```

Settings can switch the data root to the executable folder for portable use. The selected root contains the same database, images, backup, and settings structure.

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

Distribution uses an unpackaged `win-x64` ZIP containing a native launcher and a framework-dependent single-file WPF application.

The launcher detects Microsoft .NET 10 Desktop Runtime x64 and can direct the user to Microsoft's download page when it is missing. No MSIX signing or Windows App SDK runtime is required.

## 12. Testing priorities

Highest-value automated tests:

- Direct/Random/Disabled selection behavior
- ordered multi-select Positive/Negative composition
- unique random N-item selection
- empty-fragment handling and single-line separators
- schema-v1 → schema-v2 history migration
- exact multi-selection/mode/random-count history restore
- paged history queries
- prompt CRUD and image behavior
- schema-v2 backup/settings round trip
- WPF routed mouse-wheel smoke
- WPF navigation/multi-select/Prompt Library/History paging UI smoke
- published WPF application startup smoke
