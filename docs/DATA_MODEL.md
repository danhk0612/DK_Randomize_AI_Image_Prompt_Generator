# Data Model

## 1. PromptItem

Represents one reusable prompt entry.

```text
Id              GUID / TEXT primary key
Category        INTEGER enum
Title           TEXT required
PositivePrompt  TEXT nullable
NegativePrompt  TEXT nullable
Memo            TEXT nullable
ImagePath       TEXT nullable (relative application-owned path)
CreatedAtUtc    TEXT required
UpdatedAtUtc    TEXT required
```

Category values are fixed application enums in V1: Character, Artist / Style, and Additional.

## 2. Tags

```text
Tags
- Id
- Name
- NormalizedName (unique)

PromptTags
- PromptId
- TagId
PRIMARY KEY (PromptId, TagId)
```

## 3. CombinationHistory

The main history row stores the exact final edited output and timestamp.

```text
Id
PositiveText
NegativeText
CreatedAtUtc
```

Legacy schema-v1 Character/Artist snapshot columns remain for compatibility, but schema-v2 readers use the generic item relation below as the source of truth.

## 4. CombinationHistoryItems — schema v2

Stores every selected source prompt in category and selection order.

```text
HistoryId
Category
PromptId nullable
SortOrder
TitleSnapshot nullable
PRIMARY KEY (HistoryId, Category, SortOrder)
```

`PromptId` uses `ON DELETE SET NULL`. `TitleSnapshot` remains so history stays readable if the source prompt is deleted.

## 5. CombinationHistoryCategoryState — schema v2

Stores the Mixer behavior that produced the saved result.

```text
HistoryId
Category
Mode        Direct(Fixed) | Random | Disabled
RandomCount
PRIMARY KEY (HistoryId, Category)
```

The saved selected items remain the exact result shown at save time even for Random mode. `RandomCount` is restored so the next reroll uses the same requested count.

## 6. Legacy history compatibility

Schema version 1 used:

- `CombinationHistory.CharacterPromptId`
- `CombinationHistory.ArtistPromptId`
- `CombinationHistoryAdditional`

The schema-v2 migration copies those rows into the generic item/state tables and then sets `PRAGMA user_version = 2`. The legacy tables are retained so older compatibility paths do not require destructive migration.

## 7. Mixer session state

Current Mixer state is application-session state rather than durable prompt content.

```text
Per category:
- Mode
- Ordered SelectedPromptIds[]
- RandomCount

Current editable:
- Positive output
- Negative output
```

One WPF MixerView instance is retained by the shell during the application session. Prompt Library create/update/delete/add operations synchronize with this session. Mixer session state is not currently persisted across application restarts.

## 8. App settings

Settings remain small local preferences stored outside SQLite.

```text
Theme  System | Light | Dark
```

## 9. Database rules

- Foreign keys are enabled.
- Current schema version is 2.
- Migrations are forward-only and tested.
- Prompt deletion must not delete saved history output or title snapshots.
- Tag cleanup may remove orphan tags after prompt updates/deletion.
- User-entered prompt text is preserved except for composition-time trimming of the fragment boundary.
- Representative images remain application-owned files referenced by relative path.
