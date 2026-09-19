# First Release Validation Checklist

Use this checklist on the exact expanded WPF win-x64 release-candidate artifact intended for the first release.

The earlier single-selection RC is obsolete after the V1 multi-select expansion.

## Launch and shell

- [ ] Extract the WPF win-x64 ZIP to a normal user-writable folder.
- [ ] Confirm the ZIP contains only `DKRandomizeAIImagePromptGenerator.exe` and `DKRandomizeAIImagePromptGenerator.App.exe`.
- [ ] On a PC with .NET 10 Desktop Runtime x64, launch `DKRandomizeAIImagePromptGenerator.exe` and confirm the WPF app starts.
- [ ] On a PC without .NET 10 Desktop Runtime x64, launch `DKRandomizeAIImagePromptGenerator.exe` and confirm the runtime 안내 dialog appears.
- [ ] Confirm the missing-runtime dialog can open Microsoft's official .NET 10 download page.
- [ ] Confirm the executable/taskbar icon.
- [ ] Confirm Mixer, Prompt Library, History, and Settings navigation.
- [ ] Resize from wide to the minimum supported width.
- [ ] Confirm compact navigation appears on narrow windows.
- [ ] Confirm Mixer state and manually edited output survive page navigation.

## Mouse wheel / scrolling

- [x] CI routed-wheel smoke targets a nested TextBox.
- [ ] Mixer outer page scrolls while the pointer is over normal content and editable TextBoxes.
- [ ] Prompt editor scrolls over title/Positive/Negative/tags/memo controls.
- [ ] History detail and Settings scroll normally.
- [ ] Prompt gallery/list and Mixer selected-item lists scroll normally.

## Prompt Library

- [ ] Create Character, Artist / Style, and Additional prompts.
- [ ] Save Positive, Negative, tags, and memo and confirm persistence after restart.
- [ ] Add, replace, and remove a representative image; confirm the source file is unchanged.
- [ ] Verify gallery/list switching.
- [ ] Verify sorting by updated time, created time, and title.
- [ ] Verify title/prompt search and partial tag filtering.
- [ ] Click a displayed tag and confirm it applies the tag filter immediately.
- [ ] Duplicate, edit, and delete an item.
- [ ] Confirm the editor always opens at the top.
- [ ] Confirm search/tag filters stack correctly on a narrow window.
- [ ] Click **Add to Mixer** and confirm the item is appended to the correct category.
- [ ] Confirm Add to Mixer switches that category to Direct mode.
- [ ] Click Add to Mixer again and confirm no duplicate is created.
- [ ] Edit/delete an item already present in Mixer and confirm the active Mixer session stays synchronized.

## Mixer — modes and multi-select

- [ ] Confirm Direct / Random / Disabled are mutually exclusive radio modes.
- [ ] Confirm Direct enables search/remove/up/down controls.
- [ ] Confirm Random disables manual selection controls and enables random-count/reroll controls.
- [ ] Confirm Disabled disables both manual and random controls and omits the category.
- [ ] Select at least 2 prompts in each category.
- [ ] Reorder Direct items with both buttons and drag-and-drop; confirm output order changes accordingly.
- [ ] Remove an item with the per-item X and confirm output is recomposed.
- [ ] Search the category picker by title, tag, memo, Positive text, and Negative text.
- [ ] Add by double-click, remove by double-click, and drag-reorder the selected list.
- [ ] Add several search results, cancel once, then apply once; confirm cancel/apply semantics.
- [ ] Set Random count to 2+ and confirm results are unique within that category.
- [ ] Request more random items than available and confirm it safely uses all available candidates.
- [ ] Reroll one Random category and confirm other categories remain unchanged.
- [ ] Randomize all and confirm Direct selections remain unchanged.
- [ ] Confirm Positive order is Character items → Artist / Style items → Additional items.
- [ ] Confirm Negative follows the same order.
- [ ] Confirm non-empty fragments are separated by exactly one line break, with no blank line between categories.
- [ ] Edit generated text and confirm stored prompts are unchanged.
- [ ] Copy Positive and Negative independently.
- [ ] Confirm cards reflow 3-column → 2-column → 1-column as the window narrows.

## History

- [ ] Save a result containing multiple selected prompts after manually editing output.
- [ ] Confirm the upper full-width list is newest-first.
- [ ] With more than 20 records, confirm Previous/Next paging and 20-record page size.
- [ ] Confirm Character / Artist / Additional summaries include multiple titles.
- [ ] Select a record and confirm source-prompt thumbnails appear when source prompts still exist.
- [ ] Confirm final edited Positive / Negative text is exact.
- [ ] Restore to Mixer and confirm item order, modes, random counts, and final text are restored exactly.
- [ ] Reroll a restored Random category and confirm its saved random count is used.
- [ ] Delete a source prompt and confirm saved title snapshot/final text remain readable.
- [ ] Delete a history record and confirm paging remains valid.
- [ ] Clear all recent history, confirm the warning dialog, and confirm the empty state/paging reset correctly.

## Settings / schema-v2 backup

- [ ] Switch System / Light / Dark and restart to confirm persistence.
- [ ] Confirm the settings layout stacks correctly at narrow width.
- [ ] Resize/move the main window, restart, and confirm normal size/position is restored.
- [ ] Maximize the main window, restart, and confirm maximized state is restored.
- [ ] Select executable-folder portable storage, restart, and confirm the active data path changes to the executable folder.
- [ ] Switch back to LocalAppData storage, restart, and confirm the original per-user data path is used.
- [ ] Confirm changing storage mode does not silently move/delete the data in the previous location.
- [ ] Create a backup containing prompts, representative images, and multi-select history.
- [ ] Make visible changes, including deleting/changing prompts/history.
- [ ] Restore the backup.
- [ ] Confirm prompts, images, theme, history item order, modes, random counts, and final text are restored.
- [ ] Confirm displayed application version.
- [ ] Click **업데이트 확인** with no newer compatible GitHub Release and confirm a clear no-update status.
- [ ] From an older published build, verify a newer GitHub Release is detected and its win-x64 ZIP is selected.
- [ ] Confirm update installation closes the app, replaces application files, preserves LocalAppData/portable user data, and restarts the updated EXE.
- [ ] Confirm a stable build does not offer prerelease-only updates.

## Visual theme / keyboard and accessibility

- [ ] Confirm page/section titles, list selections, buttons, picker background, random-result surfaces, and ComboBoxes remain readable in both Light and Dark.
- [ ] Navigate primary controls with Tab / Shift+Tab.
- [ ] Activate buttons, radio buttons, ComboBoxes, picker lists, and history paging with keyboard input.
- [ ] Confirm focus remains visible in Light and Dark themes.
- [ ] Confirm icon-only controls expose meaningful automation names.
- [ ] Confirm navigation buttons remain understandable in compact mode via tooltip/accessibility name.

## Local release verification — expanded candidate

- [ ] Pull the final `feature/wpf-ui` HEAD.
- [ ] Run `scripts/verify-release.ps1 -Launch`.
- [ ] Local Release build succeeds.
- [ ] All current automated tests pass locally.
- [ ] Local self-contained `win-x64` Publish succeeds.
- [ ] Local WPF mouse-wheel routing smoke succeeds.
- [ ] Local expanded WPF UI smoke succeeds.
- [ ] Locally published WPF executable launches.
- [ ] Perform the real-data checks above from that exact published build.

## CI / release output

- [x] Solution Restore succeeds on the RC release commit.
- [x] Release Build succeeds on the RC release commit.
- [x] All 26 automated tests pass on the RC release commit.
- [x] Framework-dependent single-file WPF win-x64 Publish succeeds.
- [x] Native runtime-check launcher compiles successfully.
- [x] CI confirms the published package contains exactly two EXE files.
- [x] WPF routed mouse-wheel smoke succeeds.
- [x] Expanded WPF UI smoke succeeds, including multi-select, shared Mixer session, Prompt Library integration, and History paging.
- [x] Published WPF startup smoke succeeds.
- [x] Pre-RC build workflow artifact upload succeeds.
- [x] Release-branch workflow creates `DK-Randomize-AI-Image-Prompt-Generator-WPF-win-x64.zip` and attaches it to the GitHub prerelease.
- [x] Generated release tag version matches the WPF project version.
