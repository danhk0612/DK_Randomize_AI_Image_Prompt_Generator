# First Release Validation Checklist

Use this checklist on the exact WPF win-x64 artifact intended for the first release.

## Launch and shell

- [ ] Extract the WPF win-x64 ZIP to a normal user-writable folder.
- [ ] Launch `DKRandomizeAIImagePromptGenerator.exe` without installing a separate .NET runtime.
- [ ] Confirm the executable/taskbar icon.
- [ ] Confirm Mixer, Prompt Library, History, and Settings navigation.
- [ ] Resize from wide to the minimum supported width.
- [ ] Confirm compact navigation appears on narrow windows.
- [ ] Confirm no horizontal page scrolling is introduced unexpectedly.

## Mouse wheel / scrolling

- [x] CI routed-wheel smoke passes with a TextBox as the wheel target.
- [ ] Mixer outer page scrolls with the pointer over normal content and editable TextBoxes.
- [ ] Prompt editor scrolls with the pointer over title/Positive/Negative/tags/memo controls.
- [ ] History detail and Settings scroll normally.
- [ ] Prompt gallery/list scroll normally.

## Prompt Library

- [ ] Create Character, Artist / Style, and Additional prompts.
- [ ] Add Positive, Negative, tags, and memo and confirm persistence after restart.
- [ ] Add a representative image and confirm the original source is unchanged.
- [ ] Replace and remove a representative image.
- [ ] Verify gallery/list switching.
- [ ] Verify title/prompt search and tag filtering.
- [ ] Duplicate, edit, and delete an item.
- [ ] Confirm the editor always opens at the top.
- [ ] Confirm search/tag filters stack correctly on a narrow window.

## Mixer

- [ ] Verify Fixed mode uses the selected item.
- [ ] Verify Random mode chooses only from the matching category.
- [ ] Verify Disabled omits the category.
- [ ] Verify direct-selection items show thumbnail, title, memo, and tags.
- [ ] Randomize one category and confirm other current selections remain unchanged.
- [ ] Randomize all.
- [ ] Verify Positive order: Character → Artist → Additional.
- [ ] Verify Negative order: Character → Artist → Additional.
- [ ] Verify empty sections do not create repeated blank lines.
- [ ] Edit generated text and confirm stored prompts are unchanged.
- [ ] Copy Positive and Negative independently.
- [ ] Confirm cards reflow 3-column → 2-column → 1-column as the window narrows.

## History

- [ ] Save a mixer result after manually editing output.
- [ ] Confirm final edited Positive / Negative text in History.
- [ ] Restore to Mixer and confirm final text is restored exactly.
- [ ] Confirm the next Randomize All works immediately after restore.
- [ ] Delete a source prompt and confirm saved history text remains available.
- [ ] Delete a history record.

## Settings / backup

- [ ] Switch System / Light / Dark and restart to confirm persistence.
- [ ] Confirm the settings layout stacks correctly at narrow width.
- [ ] Create a backup ZIP.
- [ ] Make visible data changes.
- [ ] Restore the backup and confirm prompts, history, images, and theme.
- [ ] Confirm displayed application version.

## Keyboard and accessibility

- [ ] Navigate primary controls with Tab / Shift+Tab.
- [ ] Activate buttons, radio buttons, ComboBoxes, and lists with keyboard input.
- [ ] Confirm focus remains visible in Light and Dark themes.
- [ ] Confirm icon-only controls expose meaningful automation names.
- [ ] Confirm navigation buttons remain understandable in compact mode via tooltip/accessibility name.

## CI / release output

- [x] Solution Restore succeeds.
- [x] Release Build succeeds.
- [x] All 15 automated tests pass.
- [x] Self-contained WPF win-x64 Publish succeeds.
- [x] WPF routed mouse-wheel smoke succeeds.
- [x] WPF navigation/responsive-shell/keyboard-focus UI smoke succeeds.
- [x] Published WPF startup smoke succeeds.
- [x] Artifact upload succeeds.
- [ ] Perform the final manual smoke test from the exact artifact intended for release.
