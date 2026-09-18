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

Mixer is the default landing view.

### Selection cards

Three cards appear in this order:

1. Character
2. Artist / Style
3. Additional

Each card shows:

- representative image or placeholder
- title
- memo
- tags
- direct selection ComboBox
- Fixed / Random / Disabled mode
- per-category randomize action

Direct-selection dropdown items show thumbnail, title, memo, and tags.

A single **Randomize all** action refreshes categories currently in Random mode while preserving Fixed selections.

Responsive behavior:

- wide: 3 columns
- medium: 2 columns + Additional on the next row
- narrow: 1 column

### Result editors

Positive and Negative each contain:

- multiline editable TextBox
- independent Copy action

The final edited text can be saved to History.

## 4. Prompt Library

Top area:

- category selector: Character / Artist / Additional
- search
- tag filter
- Gallery/List toggle
- New prompt

Search and tag filter stack vertically on narrow layouts.

### Gallery mode

Cards show:

- representative image
- title
- Positive prompt preview
- tags

Selecting a card opens the editor pane.

### List mode

Rows show:

- thumbnail
- title
- Positive preview
- Negative preview

Selecting a row opens the same editor pane.

## 5. Prompt editor

The editor is an in-app side pane.

Fields:

- representative image preview
- Select image / Remove image
- title
- Positive Prompt
- Negative Prompt
- tags
- memo
- Duplicate / Delete / Save
- close button

The editor always opens scrolled to the top.

## 6. History

History is newest-first.

The list provides a compact snapshot. Selecting an entry shows:

- timestamp
- Character title
- Artist / Style title
- Additional title(s)
- final Positive text
- final Negative text
- Restore to Mixer
- Delete

History remains useful even after source prompts are renamed or deleted because final output and title snapshots are stored.

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
