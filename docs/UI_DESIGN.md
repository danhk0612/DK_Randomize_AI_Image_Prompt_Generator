# UI Design

## 1. Visual direction

The application uses a restrained, modern Windows desktop visual language aligned with WinUI 3 and Fluent Design.

Principles:

- Clean surfaces and clear hierarchy
- Native Windows controls before custom controls
- Generous but compact desktop spacing
- Rounded corners where provided naturally by WinUI
- Minimal shadows and decoration
- One accent color for primary actions
- Fluent/Symbol icons for common actions
- Representative images used where recognition benefits from visuals
- No decorative AI/robot imagery in the main workflow

## 2. Shell

Use a left `NavigationView`.

Primary destinations:

1. Mixer
2. Prompt Library
3. History
4. Settings

The title bar should integrate with the app window visually where practical. The content area uses a maximum readable width only where forms would otherwise become excessively wide.

## 3. Mixer page

The Mixer page is the default landing page.

### Selection cards

Three cards appear in order:

1. Character
2. Artist / Style
3. Additional

Each card shows:

- Representative thumbnail or placeholder
- Title or empty-state label
- Selection mode: Fixed / Random / Disabled
- Select action
- Randomize-again action when relevant

A single `Randomize all` primary action refreshes every category currently in Random mode while preserving Fixed selections.

### Result editors

Two clearly separated sections:

- Positive
- Negative

Each contains:

- Multiline editable `TextBox`
- Copy button
- Optional lightweight changed-state indicator later if useful

Generated text is ordinary editable text. There is no hidden token/chip representation in V1.

## 4. Prompt Library page

Top command area:

- Category segmented/tab selector: Character / Artist / Additional
- Search box
- Tag filter
- Gallery/List view toggle
- New prompt button

### Gallery mode

Prompt cards show:

- Representative image or placeholder
- Title
- A small number of tags
- Overflow menu (`...`)

Overflow menu:

- Use / select
- Edit
- Duplicate
- Delete

### List mode

Optimized for scanning many entries. Show thumbnail, title, tags, updated date, and compact actions.

## 5. Prompt editor

Prefer an in-app editor surface over repeated modal dialogs.

Fields:

- Representative image preview and Change/Remove actions
- Title
- Tags
- Positive Prompt
- Negative Prompt
- Memo
- Save / Cancel

Positive and Negative text areas receive most vertical space.

## 6. History page

History is presented newest-first.

Each entry shows:

- Timestamp
- Character title, if available
- Artist title, if available
- Additional title(s), if available
- Compact preview of final Positive text

Opening an entry shows final Positive and Negative text and provides:

- Restore to mixer
- Copy Positive
- Copy Negative

History must continue to display useful content even if a source prompt has later been renamed or deleted.

## 7. Settings page

Initial settings:

- Theme: System / Light / Dark
- Open application data folder
- Create backup
- Restore backup
- About/version information

Avoid exposing settings that do not yet affect implemented behavior.

## 8. Empty states

Every empty state should tell the user what to do next.

Examples:

- Empty Character library: `No character prompts yet.` + `Add character prompt`
- Random mode with no candidates: show `No prompts available` without treating it as an application error
- Empty History: explain that combinations will appear after use

## 9. Icon system

Use native Fluent/Symbol icons for actions such as:

- Add
- Edit
- Copy
- Delete
- Search
- Shuffle/randomize
- Settings
- History
- Gallery/List

### Application icon concept

The app icon represents prompt composition rather than generic AI imagery:

- Three overlapping rounded prompt cards
- A compact shuffle/combine motif
- Simple geometry that remains recognizable at 16–32 px
- One primary accent plus neutral foreground

Avoid brain, robot, wand, or spark-heavy imagery.

A vector source should be kept in `design/` and raster Windows assets derived from that source later.

## 10. Accessibility

- Keyboard navigation must follow visual order.
- Text contrast must rely on theme resources rather than hard-coded low-contrast colors.
- Icon-only buttons require accessible labels/tooltips.
- Do not communicate selection state by color alone.
- Respect system text scaling where WinUI controls provide it.
