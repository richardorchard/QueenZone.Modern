# Queen History Population Plan

The "On This Day in Queen History" feature uses `QueenHistoryEvents` as the public read model. This table is intentionally modern-owned: legacy tables are import sources, not the long-term public contract.

## Initial Sources

- `Q_TIMELINE_T`: legacy curated timeline entries.
- `QUEEN_EVENT_T`: dated Queen events.
- `Q_TOUR_DATE_T`: concerts, venues, countries, and tour notes.
- `Q_ALBUM_T`: album release dates already used by discography.
- Curated records: band member birthdays, major releases, major concerts, awards, and QueenZone milestones.

## Import Shape

Each imported row should map to:

- `Title`: short display title.
- `Summary`: neutral one-sentence archive summary.
- `EventDate`: normalized date.
- `DatePrecision`: `ExactDate`, `MonthYear`, or `YearOnly`.
- `Category`: `Birthday`, `Concert`, `Release`, `Recording`, `Award`, `TVRadio`, `SiteHistory`, or `Other`.
- `Importance`: homepage ordering weight from 0 to 100.
- `SourceType` and `SourceKey`: deterministic import identity for idempotent upserts.
- `SourceUrl`: public verification URL when available.
- `IsPublished`: false for uncertain or unreviewed imports.

Only `ExactDate` and `IsPublished = true` rows are eligible for the homepage.

## Workflow

1. Export candidate rows from each legacy source to CSV.
2. Classify date quality: exact date, month/year, year-only, invalid/unknown.
3. Normalize categories with deterministic rules:
   - `Q_TOUR_DATE_T` -> `Concert`.
   - `Q_ALBUM_T` -> `Release`.
   - Titles containing birthday/born -> `Birthday`.
   - Titles containing award/inducted/won -> `Award`.
4. Upsert into `QueenHistoryEvents` by `SourceType + SourceKey`.
5. Publish only high-confidence exact-date records at first.
6. Review uncertain records in batches before flipping `IsPublished`.
7. Add source URLs or editorial notes for high-importance records.

## Import Command

The first curated batch lives at `data/queen_history_events.csv`.

Validate the CSV without touching a database:

```powershell
powershell -File .\scripts\Import-QueenHistoryEvents.ps1 -CsvPath .\data\queen_history_events.csv -DryRun
```

Import or update rows in SQL Server:

```powershell
powershell -File .\scripts\Import-QueenHistoryEvents.ps1 -CsvPath .\data\queen_history_events.csv -ConnectionString "<connection string>"
```

The importer upserts by `SourceType + SourceKey`, so it is safe to rerun after correcting titles, summaries, dates, importance, or source URLs.

## Import Status

Initial live import completed on 2026-07-06 using `data/queen_history_events.csv`.

First run:

```text
Rows read: 252
Created: 252
Updated: 0
Unchanged: 0
```

Idempotency check:

```text
Rows read: 252
Created: 0
Updated: 0
Unchanged: 252
```

## Validation

- Count source rows, imported rows, published rows, and skipped rows.
- Confirm no duplicate `SourceType + SourceKey` values.
- Confirm no duplicate same-day same-title records.
- Spot check 10 birthdays, 10 concerts, 10 releases, and 10 random legacy timeline events.
- Verify no private fields such as `USER_ID` are exposed publicly.
- Verify homepage has at least one exact or nearby fallback record for every calendar day once the dataset matures.

## Later Admin Work

After the import rules are stable, add a protected admin screen for adding and editing curated history records. Until then, prefer repeatable import scripts and reviewed SQL migrations so the public feature remains deterministic.
