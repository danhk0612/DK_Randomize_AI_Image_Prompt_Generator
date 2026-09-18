# UI Design

## 1. Visual direction

The active WPF UI uses a restrained Windows desktop visual language.

Principles:

- clean surfaces and clear hierarchy
- native WPF controls before custom controls
- compact desktop spacing
- rounded card surfaces
- one accent color for primary actions
- representative images where visual recognition helps
- no decorative AI/robot imagery in the main workflow

## 2. Shell

The shell uses a left navigation pane with four destinations:

1. Mixer
2. Prompt Library
3. History
4. Settings

At narrow window widths the pane becomes icon-only while keeping tooltips and automation names.

## 3. Mixer

Mixer is the default landing view and remains alive as a shared session while the user navigates to other pages.

### Category cards

Three cards appear in this order:

1. Character
2. Artist / Style
3. Additional

Every card uses one radio mode:

- Direct
- Random
- Disabled

The card contains an ordered multi-selection list. Each selected/result row shows representative image, title, memo, and tags.

In Direct mode:

- **Search to select** opens a category-scoped picker.
- The picker searches title, tags, memo, Positive, and Negative together.
- Multiple prompts can be added before Apply.
- Selected prompts can be removed and moved up/down.
- Adding an existing Prompt Library item to Mixer also switches that category to Direct mode.
- Duplicate add requests are ignored.

In Random mode:

- manual selection controls are disabled
- Random count can be chosen from 1 up to the available item count
- reroll chooses unique prompts
- the current random result remains visible in the ordered list

In Disabled mode both manual and random controls are disabled and the category contributes no prompt text.

A single **Randomize all** action refreshes Random categories while preserving Direct selections.

Responsive behavior:

- wide: 3 columns
- medium: 2 columns + Additional on the next row
- narrow: 1 column

### Result editors

Positive and Negative each contain:

- multiline editable TextBox
- independent Copy action

Prompt fragments are composed Character → Artist / Style → Additional, preserving item order inside each category. Non-empty fragments are joined by one line break, not a blank line.

The final manually edited text can be saved to History. Normal page navigation does not overwrite the current Mixer selection or manual edits.

## 4. Prompt Library

Top area:

- category selector: Character / Artist / Additional
- search
- tag filter
- Gallery/List toggle
- New prompt

Search and tag filter stack vertically on narrow layouts.

### Gallery mode

Cards show representative image, title, Positive preview, and tags.

### List mode

Rows show thumbnail, title, Positive preview, and Negative preview.

Selecting a card/row opens the same editor pane.

## 5. Prompt editor

The editor is an in-app side pane.

Fields/actions:

- representative image preview
- Select image / Remove image
- title
- Positive Prompt
- Negative Prompt
- tags
- memo
- Duplicate
- Delete
- Add to Mixer
- Save
- close

**Add to Mixer** immediately adds the stored item to its category in the shared Mixer session. An already-selected item is ignored.

The editor always opens scrolled to the top.

## 6. History

History is newest-first.

The upper area is a full-width list with 20 records per page. Each row shows:

- local timestamp
- Character selection summary
- Artist / Style selection summary
- Additional selection summary
- Positive preview

Previous/Next controls navigate pages.

Selecting a record opens the detail area below. Detail shows:

- selection mode and saved/random count for each category
- ordered title summaries
- current representative thumbnails for source prompts that still exist
- final Positive text
- final Negative text
- Restore to Mixer
- Delete

Restore reproduces selection order, category modes, random counts, and exact final edited text. Deleted source prompts keep title snapshots and final output even when their current thumbnail is no longer available.

## 7. Settings

Settings includes:

- Theme: System / Light / Dark
- Create backup
- Restore backup
- application version

Theme and backup controls stack below their descriptions on narrow layouts.

## 8. Scrolling

Outer WPF page/editor ScrollViewers use `PreviewMouseWheel` routing so scrolling remains consistent even while the pointer is over nested editable controls.

The Prompt Library gallery/list uses the native ListBox scrolling behavior.

## 9. Empty and error states

Empty views explain why nothing is shown and what action is available next.

Errors from prompt persistence, images, settings, backup, and restore must be surfaced to the user rather than silently ignored.

## 10. Accessibility

- keyboard navigation follows visual order
- theme resources provide text/background contrast
- icon-only controls expose automation names and tooltips
- repeated controls such as per-category Randomize and Copy have category-specific automation names
- compact navigation retains tooltips and automation names
- selection state is not communicated by color alone

## 11. Application icon

The icon represents prompt composition:

- three prompt cards
- shuffle/combine motif
- simple geometry suitable for small Windows icon sizes
- restrained accent/neutral palette
