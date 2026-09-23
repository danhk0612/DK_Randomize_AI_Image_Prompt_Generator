# Requirements

## 1. Product goal

A Windows desktop application for storing reusable AI image-generation prompts and quickly building an editable prompt result from fixed or random selections.

The application does not generate images in V1. It prepares prompts for use in other services and tools.

## 2. Supported platform

- Windows 10 version 1809 (build 17763) or later
- Windows 11
- Desktop-only application

## 3. Prompt categories

Exactly three top-level categories are supported in V1:

1. Character
2. Artist / Style
3. Additional

Each prompt item stores:

- Stable unique ID
- Category
- Title
- Representative image
- Positive prompt
- Negative prompt
- Tags
- Memo / notes
- Created date
- Updated date

Positive prompt, negative prompt, image, tags, and memo are optional. Title is required.

## 4. Prompt management

For each category, the user can:

- Create an item
- View items as a gallery or list
- Search by title and prompt text
- Filter by tags
- Edit an item
- Duplicate an item
- Delete an item with confirmation
- Change or remove the representative image
- Add an existing saved prompt directly to the active Mixer selection

Representative images are copied into application-managed local storage. The original source file must never be modified.

## 4.1 Bulk prompt import

Prompt Library supports importing multiple UTF-8 `.txt` files from either a selected folder or a multi-file selection.

Import rules:

- The current Prompt Library category is used for every selected file.
- The TXT file name without extension becomes the prompt title.
- Supported sections are `[Positive]`, `[Negative]`, `[Tags]`, and `[Memo]`.
- Title plus at least one non-empty Positive or Negative prompt is required.
- Tags, memo, and representative image are optional.
- Tags are comma-separated.
- A same-basename image in the same source folder is imported automatically when its extension is PNG, WebP, JPG, JPEG, or BMP.
- Missing images are not errors.
- A title already present in the same category is rejected without overwriting existing data.
- Each source file is processed independently. One failure must not stop the remaining files.
- The import UI reports waiting, processing, success, or a short failure reason per file and shows a final success/failure count.

## 5. Mixer

Each category has exactly one selection mode:

- Direct: use the ordered manually selected items
- Random: choose the requested number of unique eligible items
- Disabled: omit the category

V1 permits multiple selected items in every category.

For each category:

- Direct selection keeps an ordered list of manually selected prompts.
- Random selection chooses a user-selected number of unique prompts.
- Disabled omits the category.
- Selection order is preserved and controls prompt composition order within that category.

The user can:

- Add or remove multiple items in a category.
- Reorder directly selected items.
- Randomize one category independently.
- Randomize all Random-mode categories at once.
- Set the random item count per category.
- Keep Direct selections unchanged while other categories are randomized.

## 6. Output composition

Two independent editable outputs are generated:

- Positive
- Negative

Default positive composition order:

1. Character items in selection order
2. Artist / Style items in selection order
3. Additional items in selection order

Negative composition follows the same category and selection order.

Rules:

- Every non-empty prompt fragment is joined with exactly one line break.
- Empty prompt sections are skipped.
- Empty sections must not create extra line breaks.
- Stored prompt text is copied verbatim; the application must not insert commas, weights, syntax, or model-specific formatting.
- The generated output text areas are editable.
- Editing generated output must not modify stored prompt items.
- Recombining replaces the current generated output.
- Positive and Negative each have an independent Copy action.

## 7. History

The application stores recent combinations locally.

Each history record contains:

- Ordered selected prompt IDs and title snapshots for every category
- Selection mode for every category
- Random-count setting for every category
- Final positive text
- Final negative text
- Created date

History must store the final edited text, selection order, mode, and random-count state so a saved result can be restored exactly. If a source prompt is later deleted, the title snapshot and final output remain readable.

V1 displays history newest-first in a paged list with 20 records per page. Selecting a record shows the currently available representative thumbnails for its source prompts.

## 8. Theme and appearance

The application supports:

- Follow system theme
- Light theme
- Dark theme

The UI follows a restrained Fluent-style desktop design with clear hierarchy, generous spacing, rounded surfaces, and standard Windows iconography.

## 9. Local data

Application data is stored under the current user's local application data directory by default. Settings can switch the data root to the executable folder for portable use.

Default structure:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\
│  └─ prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Structured data uses SQLite. Representative images are stored as files and referenced by relative path from the database.

Changing the selected data root takes effect on the next launch and must not silently move or delete the previous data root.

## 10. Backup and restore

V1 includes manual backup and restore of application-owned data.

A backup contains:

- Database
- Representative images
- Settings that are safe and useful to restore

Backup/restore must not include unrelated files from the user's machine.

## 11. Updates

V1 can check GitHub Releases for newer compatible versions. Updates are user-initiated and preserve application-owned user data while replacing program files.

Stable builds do not automatically offer prerelease-only updates.

## 12. Out of scope for V1

The following are deliberately excluded from V1:

- Direct AI image generation
- OpenRouter integration
- Model-specific prompt conversion
- Automatic prompt rewriting
- Cloud synchronization
- Account system
- Multi-device synchronization

These may be considered only after the local prompt-management and mixing workflow is stable.
