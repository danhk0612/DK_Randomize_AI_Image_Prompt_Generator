# DK Randomize AI Image Prompt Generator

Windows desktop application for managing and combining reusable AI image-generation prompts.

## Status

Initial project setup in progress.

## Core concept

The application manages three prompt groups independently:

- Character prompts
- Artist/style prompts
- Additional prompts

Each prompt item can contain a title, representative image, positive prompt, negative prompt, tags, and notes. The mixer can use a fixed item, a random item, or skip a group, then produces editable positive/negative results that can be copied for use in other image-generation tools.

## Planned platform

- Windows 10 1809+ / Windows 11
- C# / .NET 10
- WinUI 3
- Windows App SDK 2.4
- SQLite for local structured data

Detailed requirements and architecture will be maintained under `docs/`.
