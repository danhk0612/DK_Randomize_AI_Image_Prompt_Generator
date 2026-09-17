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
CreatedAtUtc    TEXT/INTEGER required
UpdatedAtUtc    TEXT/INTEGER required
```

Category values are application enums, not user-defined categories in V1.

## 2. Tag

```text
Id    GUID / TEXT primary key
Name  TEXT required, unique using normalized comparison
```

## 3. PromptTag

Many-to-many link table.

```text
PromptId  TEXT foreign key -> PromptItem.Id
TagId     TEXT foreign key -> Tag.Id
PRIMARY KEY (PromptId, TagId)
```

## 4. CombinationHistory

Stores the result exactly as it existed when saved to history.

```text
Id                    GUID / TEXT primary key
CharacterPromptId     TEXT nullable
ArtistPromptId        TEXT nullable
PositiveText          TEXT required
NegativeText          TEXT required
CreatedAtUtc          TEXT/INTEGER required
```

Additional source prompts are normalized into a separate relation so the model can expand to multiple Additional prompts later.

## 5. CombinationHistoryAdditional

```text
HistoryId   TEXT foreign key -> CombinationHistory.Id
PromptId    TEXT nullable
SortOrder   INTEGER required
TitleSnapshot TEXT nullable
PRIMARY KEY (HistoryId, SortOrder)
```

A title snapshot is retained for meaningful history display even if the source prompt is later removed.

For the same reason, implementations may retain title snapshots for Character and Artist in history when schema implementation begins. The final schema must preserve historical readability without requiring source rows to exist.

## 6. Selection state

Current mixer state is application state rather than durable prompt content.

```text
CharacterMode      Fixed | Random | Disabled
CharacterPromptId  nullable
ArtistMode         Fixed | Random | Disabled
ArtistPromptId     nullable
AdditionalMode     Fixed | Random | Disabled
AdditionalPromptIds collection (V1 count 0..1)
```

Whether mixer state persists across app restarts will be decided when Settings persistence is implemented; it must not be mixed into prompt records.

## 7. App settings

Settings are small local preferences and may be stored outside SQLite.

Initial fields:

```text
Theme  System | Light | Dark
```

Add fields only when a corresponding user-facing behavior exists.

## 8. Database rules

- Foreign keys enabled.
- Prompt deletion must not delete history output text.
- Tag cleanup may remove orphan tags after prompt updates/deletion.
- Schema version is explicit from version 1.
- Migrations must be forward-only and tested once schema code is introduced.
- User-entered prompt text is preserved exactly except for database encoding/storage requirements.
