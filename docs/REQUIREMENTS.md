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

Representative images are copied into application-managed local storage. The original source file must never be modified.

## 5. Mixer

Each category has one selection mode:

- Fixed: use the selected item
- Random: randomly choose an eligible item
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

1. Character positive prompt
2. One blank line
3. Artist positive prompt
4. One blank line
5. Additional positive prompt

Negative composition follows the same category order.

Rules:

- Empty prompt sections are skipped.
- Empty sections must not create duplicate blank lines.
- Stored prompt text is copied verbatim; the application must not insert commas, weights, syntax, or model-specific formatting.
- The generated output text areas are editable.
- Editing generated output must not modify stored prompt items.
- Recombining replaces the current generated output.
- Positive and Negative each have an independent Copy action.

## 7. History

The application stores recent combinations locally.

Each history record contains:

- Character prompt ID, if any
- Artist prompt ID, if any
- Additional prompt IDs
- Final positive text
- Final negative text
- Created date

History must store the final edited text, not only the source prompt IDs, so a manually adjusted result can be restored exactly.

The initial history limit is configurable later; V1 may use a sensible fixed limit.

## 8. Theme and appearance

The application supports:

- Follow system theme
- Light theme
- Dark theme

The UI follows a restrained Fluent-style desktop design with clear hierarchy, generous spacing, rounded surfaces, and standard Windows iconography.

## 9. Local data

Application data is stored under the current user's local application data directory.

Suggested structure:

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\
│  └─ prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Structured data uses SQLite. Representative images are stored as files and referenced by relative path from the database.

## 10. Backup and restore

V1 includes manual backup and restore of application-owned data.

A backup contains:

- Database
- Representative images
- Settings that are safe and useful to restore

Backup/restore must not include unrelated files from the user's machine.

## 11. Out of scope for V1

The following are deliberately excluded from V1:

- Direct AI image generation
- OpenRouter integration
- Model-specific prompt conversion
- Automatic prompt rewriting
- Cloud synchronization
- Account system
- Multi-device synchronization

These may be considered only after the local prompt-management and mixing workflow is stable.
