# Extracting quotes/dates/trivia from book PDFs

Standard approach for pulling new `queen_quotes.csv` / `queen_history_events.csv`
candidates out of a scanned book PDF (e.g. band biographies, visual documentaries),
for review before merging into `./data/`.

## Prefer `pdftotext` over vision-based PDF reading

Claude Code's `Read` tool on a PDF renders each page as an image and reads it via
vision. For scanned books full of photos (concert shots, backstage photos, etc.)
this repeatedly tripped the Claude API's content-filtering policy on unpredictable
page ranges, which is fatal for a long unattended extraction run — a background
agent working through a few hundred pages this way will stall over and over on
essentially random ranges with no way to reliably retry past them.

Most commercially-typeset book PDFs (including scanned ones) carry a real text
layer even though they look like photographs of pages. Check for one and use it
instead:

```bash
# Whole book, preserving rough column layout
pdftotext -layout "path/to/book.pdf" book_fulltext.txt

# Just a page range (max ~20-30 pages per file keeps things manageable)
pdftotext -layout -f 38 -l 49 "path/to/book.pdf" pages_38-49.txt
```

`pdftotext` ships with poppler and is already on PATH in this dev environment
(`mingw64/bin/pdftotext`). Read the resulting `.txt` file with the normal `Read`
tool — plain text, no page-image rendering, no content-filter risk, and much
cheaper than vision reads.

Caveats:
- Layout is often mangled (multi-column pages interleave, captions and pull-quotes
  land mid-sentence, OCR-adjacent garbling on stylised fonts) — treat it as noisy
  raw material to interpret, not a clean transcript. Cross-reference against
  context (surrounding dates, names) when a line looks corrupted.
- If a given page range comes back empty or as garbage even with `-layout`, that
  page range is genuinely image-only (no text layer) — that's the one case where
  falling back to `Read` with the `pages` parameter (vision) on just that narrow
  range is worth it, since it's no longer the default path for the whole book.

## Workflow

1. Extract full or page-ranged text with `pdftotext -layout` into the scratchpad
   directory.
2. Read the `.txt` file(s) — not the PDF — and pull candidate quotes, dated
   events, and dateless trivia.
3. Dedupe against `./data/queen_quotes.csv` and `./data/queen_history_events.csv`
   (and any other book's already-extracted candidate files) by substance, not
   exact string match — the same anecdote often gets rephrased across sources.
4. Write curated candidates to `docs/backlog/<book-slug>-extraction/` as three
   files matching the site's import schemas:
   - `new_quotes.csv` — `Text,WhoSaid,Context,SourceType,SourceKey`
   - `new_history_events.csv` — `Title,Summary,EventDate,DatePrecision,Category,Importance,SourceType,SourceKey,SourceUrl`
   - `new_trivia.md` — bulleted dateless facts, one line of page/reason context each
   Use a new `SourceType` literal per book (e.g. `VisualDocumentaryBook`), and add
   the matching enum member to `QuoteSourceType` and `QueenHistoryEventSourceType`
   (`src/QueenZone.Data/Quotes/QuoteSourceType.cs`,
   `src/QueenZone.Data/History/QueenHistoryEventSourceType.cs`) before import — no
   migration needed, these aren't DB-backed lookups.
5. Never write directly to `./data/*.csv` or run the importers as part of this
   pass — it's a curation step for a human to review first.

See `docs/backlog/visual-documentary-extraction/` for a worked example (from
*Queen: A Visual Documentary*, Ken Dean & Chris Charlesworth).
