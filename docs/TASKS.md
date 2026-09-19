# Tasks

## Milestone 0 — Bootstrap

- [x] Initialize repository
- [x] Define V1 requirements
- [x] Define architecture
- [x] Define UI design direction
- [x] Define initial data model
- [x] Add initial desktop project scaffold
- [x] Add application icon
- [x] Add shell/navigation
- [x] Add build workflow

## Milestone 1 — Domain and mixer core

- [x] Add prompt domain models and enums
- [x] Add combination models
- [x] Implement `CombinationService`
- [x] Test Fixed / Random / Disabled
- [x] Test Positive / Negative composition
- [x] Test empty prompt sections

## Milestone 2 — Local persistence

- [x] SQLite initialization
- [x] Schema version 1
- [x] Prompt CRUD repository
- [x] Tags and search
- [x] History persistence
- [x] Repository tests

## Milestone 3 — Prompt Library

- [x] Gallery/list
- [x] Categories
- [x] Search
- [x] Partial tag filter and clickable tag shortcuts
- [x] Sort by updated/created/title
- [x] Create/edit/duplicate/delete
- [x] Representative image management

## Milestone 4 — Mixer

- [x] Fixed / Random / Disabled
- [x] Direct item selection
- [x] Thumbnail/title/memo/tags display
- [x] Per-category randomize
- [x] Randomize all
- [x] Editable Positive / Negative
- [x] Copy actions
- [x] Save final output to history

## Milestone 5 — History / Settings

- [x] History list/detail
- [x] Restore history to mixer
- [x] Theme setting
- [x] Select LocalAppData or executable-folder portable data storage
- [x] Remember main-window size, position, and maximized state
- [x] GitHub Releases update check/download/self-update
- [x] Tag-driven GitHub Release ZIP workflow
- [x] Backup
- [x] Restore
- [x] About/version

## Milestone 6 — WPF migration and polish

- [x] Preserve shared Core/data behavior
- [x] Rebuild active UI in WPF
- [x] Preserve existing LocalAppData
- [x] Resolve mouse-wheel scrolling issue
- [x] Add routed-wheel CI smoke test
- [x] Restore responsive Mixer layout
- [x] Restore responsive Prompt Library layout
- [x] Restore responsive History/Settings layout
- [x] Restore compact navigation layout
- [x] WPF accessibility code pass
- [x] WPF navigation/keyboard automated smoke pass
- [x] Local release verification script pass
- [ ] WPF keyboard/focus visual manual smoke pass
- [ ] Final WPF real-data smoke pass
- [ ] Prepare first release

## Milestone 7 — V1 multi-select mixer expansion

### 7.1 Core / database
- [x] Multi-select model for Character / Artist / Additional
- [x] Preserve selection order
- [x] Random count and unique random selection
- [x] Single-newline prompt composition
- [x] History schema v2 generic category items/state
- [x] v1 → v2 history migration
- [x] Core/persistence regression tests

### 7.2 Mixer UI
- [x] Direct / Random / Disabled radio modes
- [x] Disable manual picker in Random / Disabled modes
- [x] Multi-selected item display and ordering
- [x] Direct remove button per selected item
- [x] Drag reorder in Mixer and picker selected list
- [x] Reverse double-click removal in picker
- [x] Random count UI
- [x] Integrated category search / add / apply / cancel picker

### 7.3 Prompt Library integration
- [x] Shared mixer session state
- [x] Add-to-mixer action from Prompt Library
- [x] Ignore duplicate additions
- [x] Switch target category to Direct mode when adding

### 7.4 History UI
- [x] Top full-width paged history list
- [x] Multi-selection summaries
- [x] Selected prompt thumbnails
- [x] Exact multi-selection/mode restore
- [x] Clear all recent history with confirmation

### 7.5 Verification / release candidate
- [x] v1 database migration smoke
- [x] backup/restore regression
- [x] expanded WPF UI smoke
- [x] final Light/Dark UI visual polish pass
- [x] docs/release checklist refresh
- [x] create and publish `v1.0.0-rc.1` release candidate

## Deferred

- Multiple Additional prompts in the UI
- Tag-constrained random pools
- Direct image generation
- OpenRouter integration
- Prompt rewriting/conversion
- Cloud sync
