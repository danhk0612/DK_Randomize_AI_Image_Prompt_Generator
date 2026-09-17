# First Release Validation Checklist

Use this checklist on a clean Windows test environment before publishing the first GitHub Release.

## Launch and shell

- [ ] Extract the `win-x64` publish ZIP to a normal user-writable folder.
- [ ] Launch `DKRandomizeAIImagePromptGenerator.exe` without installing a separate .NET runtime.
- [ ] Confirm the application icon is visible on the executable and taskbar.
- [ ] Confirm Mixer, Prompt Library, History, and Settings navigation works.
- [ ] Confirm the window remains usable at common Windows display scaling values.

## Prompt Library

- [ ] Create one Character prompt with Positive and Negative text.
- [ ] Create one Artist / Style prompt.
- [ ] Create one Additional prompt.
- [ ] Add tags and memo text and verify they persist after restarting the app.
- [ ] Add a representative image and verify the original source image is unchanged.
- [ ] Replace and remove a representative image.
- [ ] Verify gallery/list switching.
- [ ] Verify title/prompt search and tag filtering.
- [ ] Duplicate, edit, and delete an item.

## Mixer

- [ ] Verify Fixed mode uses the selected item.
- [ ] Verify Random mode chooses an item from the matching category.
- [ ] Verify Disabled mode omits the category.
- [ ] Randomize one category and confirm the other current selections remain unchanged.
- [ ] Randomize all Random-mode categories.
- [ ] Verify Positive order: Character → Artist → Additional.
- [ ] Verify Negative order: Character → Artist → Additional.
- [ ] Verify empty prompt sections do not create repeated blank lines.
- [ ] Edit the generated text directly and confirm stored prompt items are not changed.
- [ ] Copy Positive and Negative independently and paste them into another application.

## History

- [ ] Save a mixer result after manually editing the output.
- [ ] Confirm the final edited Positive / Negative text appears in History.
- [ ] Restore the history entry to the mixer and confirm the final text is restored exactly.
- [ ] Delete a source prompt and confirm the saved history text remains available.

## Settings

- [ ] Switch System / Light / Dark themes and restart the application to confirm persistence.
- [ ] Create a backup ZIP.
- [ ] Make visible data changes after the backup.
- [ ] Restore the backup and confirm prompts, history, images, and theme settings are restored.
- [ ] Confirm the displayed application version is present.

## Keyboard and accessibility smoke test

- [ ] Navigate primary controls with Tab / Shift+Tab.
- [ ] Activate buttons and selectable controls with keyboard input.
- [ ] Confirm focus remains visible in both Light and Dark themes.
- [ ] Confirm icon-only controls expose meaningful accessible names.

## Release output

- [ ] GitHub Actions Restore step succeeds.
- [ ] GitHub Actions Release Build step succeeds.
- [ ] All automated tests pass.
- [ ] `win-x64` self-contained Publish succeeds.
- [ ] Published artifact is uploaded and can be downloaded/extracted.
- [ ] Perform the launch smoke test from the exact artifact intended for release.
