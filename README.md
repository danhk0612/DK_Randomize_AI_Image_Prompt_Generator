# DK Randomize AI Image Prompt Generator

Windows desktop application for storing reusable AI image-generation prompts and quickly combining them into editable Positive / Negative output.

## Main features

- Manage three independent prompt groups:
  - Character
  - Artist / Style
  - Additional
- Store a title, representative image, Positive prompt, Negative prompt, tags, and notes for each item.
- Browse prompts in gallery or list view.
- Search prompt text and filter by tag.
- Create, edit, duplicate, and delete prompt items.
- Choose Fixed / Random / Disabled mode independently for each prompt group.
- Randomize one group or all random-mode groups.
- Edit the combined Positive / Negative text before copying it.
- Copy Positive and Negative results independently.
- Save the final edited result to recent history and restore it later.
- Follow the Windows system theme or force Light / Dark mode.
- Back up and restore local data as a ZIP file.

The application does **not** generate images directly. Its role is to prepare prompt text for use in other image-generation tools and services.

## Data storage

Application data is local to the current Windows user.

```text
%LOCALAPPDATA%\DK Randomize AI Image Prompt Generator\
├─ data\prompts.db
├─ images\
├─ backups\
└─ settings.json
```

Representative images are copied into application-managed storage. The original image files are not modified.

## Platform and development stack

- Windows 10 1809+ / Windows 11
- C# / .NET 10
- WinUI 3
- Windows App SDK 2.5.1
- SQLite (`Microsoft.Data.Sqlite`)
- x64 / ARM64 project targets

## Development

Open `DK_Randomize_AI_Image_Prompt_Generator.sln` and build the solution with Visual Studio or the .NET CLI on Windows.

GitHub Actions validates restore, Release build, and automated tests on Windows runners.

Detailed product requirements, architecture, UI direction, data model, task status, and current implementation state are maintained under [`docs/`](docs/).
