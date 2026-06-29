# Forum Read Performance Benchmark - 2026-06-29

Issue: <https://github.com/richardorchard/QueenZone.Modern/issues/85>

This benchmark compares the current public legacy forum read path with equivalent reads against the populated `ModernForum*` tables in `queenzone-db`.

## Scripts

- `docs/sql/005-forum-read-performance-benchmark.sql`
  - Read-only benchmark harness.
  - Runs each old/new query shape three times and reports per-run and aggregate timings.
- `docs/sql/006-modern-forum-read-path.sql`
  - Adds modern read-path stored procedures.
  - Adds targeted covering indexes for category detail, category topic pages, and forum sitemap output.

## Sample Inputs

- Forum: `10` (`Queen -  General Discussion`)
- Topic: `1495269` (`Thoughts on Thor Arnold and Lee Nolan's eleven year friendship with Freddie`)
- Category topic page size: `25`
- Topic post page size: `15`
- Sitemap page size: `50,000`

The sampled topic has `6,276` imported posts, which makes it a useful stress case for topic page reads.

## Live Results After Applying `006-modern-forum-read-path.sql`

| Area | Sample | Legacy avg ms | Modern avg ms | Notes |
| --- | ---: | ---: | ---: | --- |
| Forum index categories | all categories | 0.0 | 0.3 | Both paths are trivial. |
| Category detail | forum 10 | 0.0 | 2.3 | Modern was ~335 ms before `IX_ModernForumThread_CategoryStarter_Latest`; now effectively instant. |
| Category topics | forum 10 page 1 | 1,836.0 | 222.0 | Modern returns the same `TotalRecords` value as legacy: `30,897`. |
| Category topics | forum 10 page 100 | 2,121.3 | 222.3 | Modern page 100 benefits from the category-page covering index. |
| Topic posts | topic 1495269 page 1 | 6,295.7 | 5.7 | Modern returns the same total post count: `6,276`. |
| Topic posts | topic 1495269 page 100 | 7,190.3 | 18.3 | Modern stays fast on deeper offsets for this large thread. |
| Thread count | archive stats | 625.7 | 208.7 | Counts differ because the corrected import has `89,070` modern threads versus legacy view count `88,679`. |
| Forum sitemap file 1 | first sitemap file | 606.3 | 1,562.0 | Legacy only returns `6,289` `parent_id = 0` rows; modern returns the first `50,000` of `89,070` corrected thread rows. |

## Important Parity Notes

- `Q_FORUM_VIEW_PAGE_SP` filters displayed rows to validated users, but its `@TotalRecords` output counts all `topic_starter = 1` rows in the forum. The benchmark mirrors that behavior for the modern category page comparison.
- Legacy sitemap generation currently uses only `Q_FORUM_TOPIC_PARENT_ID = 0`, which misses most imported threads. The modern sitemap count is intentionally much larger because the corrected import uses the reviewed modern thread set.
- The modern topic-post path includes attachment fields (`Attachment`, `FileSize`, `AttachCount`) so those values remain available for the upcoming attachment exposure work.

## Applied Live

`docs/sql/006-modern-forum-read-path.sql` was applied successfully to `queenzone-db` on 2026-06-29.
