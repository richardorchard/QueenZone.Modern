# Legacy Table Map

Source: `MAIN2_DB` schema export in `docs/legacy/db-schema.txt` (2026-06-22).

The legacy schema contains 129 tables. This file focuses on the tables relevant to the new read-only public site.

## Public Content

| Table | Purpose | Initial Treatment |
| --- | --- | --- |
| `NEWS_T` | News items. | Phase 1. |
| `Q_ARTICLE_T` | Queen articles. | Phase 2. |
| `Q_ARTICLE_CATEGORY_T` | Article categories. | Phase 2. |
| `Q_BIO_T` | Biography pages. | Phase 2. |
| `Q_ALBUM_T` | Albums. | Phase 2. |
| `Q_ALBUM_SONG_T` | Songs and lyrics/notes. | Phase 2, with copyright review for lyrics. |
| `Q_ALBUM_REVIEW_T` | Album reviews. | Later content review. |
| `Q_ARTIST_T` | Artist lookup. | Phase 2 support table. |
| `PIC_FILES_T` | Public picture records. | Phase 3 after asset audit. |
| `PIC_CAT_T` | Picture categories. | Phase 3. |
| `Q_KNOWLEDGE_BASE_T` | FAQ/knowledge base entries. | Phase 2. |
| `Q_KNOWLEDGE_BASE_CATEGORY_T` | FAQ categories. | Phase 2. |
| `Q_LINK_CAT_T` | Link categories. | Phase 2 if data is still useful. |
| `QUEEN_QUOTE_T` | Quotes. | Phase 2. |
| `QUEEN_FEATURED_SITE_T` | Featured sites. | Phase 2 after link validation. |
| `Q_TIMELINE_T` | Timeline entries. | Phase 3. |
| `QUEEN_EVENT_T` | Queen event data. | Phase 3. |
| `Q_TOUR_DATE_T` | Tour dates. | Phase 3 archive. |
| `Q_YOUTUBE_T` | YouTube/video records. | Phase 3 after embed validation. |
| `VIDEO_T` | Legacy video records. | Phase 3 after validation. |
| `FREDDIE_T` | Freddie tribute entries. | Later, public review required. |

## Community Archive

| Table | Purpose | Initial Treatment |
| --- | --- | --- |
| `Q_FORUM_T` | Forum categories. | Historical import source. Public `/forum` reads use `ModernForum*` by default. |
| `Q_FORUM_TOPIC_T` | Forum topics and replies. | Historical import source. Public topic/post pages use `ModernForum*` by default. |
| `Q_FORUM_TOPIC_SUBJECT_T` | Forum subject metadata. | Historical import source for modern forum projection. |
| `Q_STAGE_T` | Fan-submitted song performances ("fan stage"). | Read-only archive, wired on `/fan-performances` (`DISPLAY = 1` rows only). |

### `Q_STAGE_T` columns

| Column | Type | Notes |
| --- | --- | --- |
| `Q_STAGE_ID` | `smallint` | Primary key. |
| `TITLE` | `varchar(100)` | Track title. |
| `PERFORMED_BY` | `varchar(100)` | Performer/band name. |
| `DESCRIPTION` | `varchar(1000)` | Free-text notes from the submitter. |
| `URL` | `varchar(200)` | Bare audio filename (e.g. `2014417798057369.mp3`), not a path — served from the `songfiles` Azure Blob Storage container behind `pictures.queenzone.org`. |
| `THESIZE` | varies | File size in bytes, stored as text by `Q_STAGE_T_PAGE_SP`. |
| `DATE_ADDED` | `smalldatetime` | Submission date; archive sort key (`DESC`). |
| `DISPLAY` | `tinyint` | Public visibility gate; only `1` is shown. |
| `CONTACT` | `varchar(300)` | Legacy contact field; always empty in current data but excluded from the read model defensively (PII-shaped column). |
| `USER_ID` | `int` | Legacy submitter account id; not exposed. |
| `ALLOW_RATING` | `tinyint` | Leftover legacy rating-feature flag; not used (no rating feature in scope). |

Related stored procedures: `Q_STAGE_T_LIST_SP` (unfiltered full list, unused by the modern app), `Q_STAGE_T_PAGE_SP` (paged, `DISPLAY = 1` only, ordered by `DATE_ADDED DESC` — used by `LegacyFanPerformanceRepository`), `Q_STAGE_T_DISPLAY_SP` (single row by id, does **not** filter `DISPLAY` — unused since the modern feature has no detail page).

`Q_STAGE_T_PAGE_SP`'s own `@ItemCount` output counts every row in the table, not just visible ones, so `LegacyFanPerformanceRepository.GetVisibleCountAsync` runs a direct `COUNT(*) WHERE DISPLAY = 1` instead of trusting it.

### `Q_FORUM_T` columns

| Column | Type | Notes |
| --- | --- | --- |
| `Q_FORUM_ID` | `int` | Primary key. |
| `Q_FORUM_NAME` | `varchar(50)` | Board name shown in the archive index. |
| `Q_FORUM_DESCRIPTION` | `varchar(200)` | Board description. |
| `Q_FORUM_POST_COUNT` | `int` | Denormalised post total maintained by legacy app logic. |
| `Q_FORUM_LAST_POST` | `datetime` | Last activity timestamp for the board. |
| `FORUM_ORDER` | `tinyint` | Sort order for category lists (`Q_LIST_FORUM_SP`, `Q_FORUM_MAIN_SP`). |
| `TITLE_WORDS` | `varchar(50)` | Legacy SEO/search helper text. |

### `Q_FORUM_TOPIC_T` columns (selected)

| Column | Type | Notes |
| --- | --- | --- |
| `Q_FORUM_TOPIC_ID` | `int` identity | Topic/reply id. |
| `Q_FORUM_ID` | `tinyint` | Parent forum category. |
| `TOPIC_SUBJECT` | `char` | Thread title (trim on read). |
| `TOPIC_REPLIES` | `smallint` | Reply count on parent topics. |
| `TOPIC_LAST_POST` | `smalldatetime` | Last activity on the thread. |
| `Q_FORUM_TOPIC_PARENT_ID` | `int` | `0` marks a top-level thread; non-zero marks a reply. |
| `TOPIC_STARTER` | `tinyint` | Used by `Q_FORUM_TOPIC_NO_PARENT_V` (`1` = starter post). |
| `STICKY` | `tinyint` | Pinned thread marker. |
| `DISCOGRAPHY` | `tinyint` | Legacy moderation/filter flag (`2` excluded from hot-topic procs). |

### Useful forum views

| View | Purpose |
| --- | --- |
| `Q_FORUM_TOPIC_V` | Top-level threads with username, reply count, forum name. |
| `Q_FORUM_TOPIC_NO_PARENT_V` | Starter posts (`TOPIC_STARTER = 1`). |
| `dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V` | Pre-aggregated thread count per forum. Sum for site-wide thread totals. |
| `FORUM_VIEW_V` | Forum topics joined to users (used by `Q_FORUM_VIEW_SP`). |

### Legacy forum read patterns (`LegacyForumRepository`)

`LegacyForumRepository` remains available when `ForumData:UseModernForumReads` is `false`. Production defaults to `ModernForumRepository` against imported `ModernForum*` tables (`docs/sql/006-modern-forum-read-path.sql`).

If using the legacy path, avoid ad-hoc `COUNT(*)` scans on `Q_FORUM_TOPIC_T` in production. The table is large and causes App Service timeouts.

| Page need | Preferred legacy source |
| --- | --- |
| Category index (`/forum`) | Direct read of `Q_FORUM_T` ordered by `FORUM_ORDER` (same shape as `Q_LIST_FORUM_SP`). |
| Board/post hero stats | Derive board count and post total from the category read (`Q_FORUM_POST_COUNT` per row). Sum thread totals from `dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V`. |
| Category header (`/forum/{id}/{slug}`) | `Q_FORUM_T` for one board; optional `OUTER APPLY` for latest thread title on a single row only. |
| Paged topic list | `Q_FORUM_VIEW_PAGE_SP` with `@TotalRecords` output. Do not add a separate manual `COUNT(*)` query. |
| Thread detail (`/forum/topic/{id}/{slug}`) | `Q_FORUM_TOPIC_NEW_SP` for paged posts in chronological order. Pass `@USER_ID = 0` for anonymous archive reads. |

### Modern forum read patterns (`ModernForumRepository`)

Default production path (`ForumData:UseModernForumReads = true`):

| Page need | Preferred modern source |
| --- | --- |
| Category index / stats | `ModernForum_GetCategories` and related read-stat procedures |
| Category topics | Modern category topic page procedures with covering indexes |
| Thread detail | Modern topic/post page procedures |
| Forum sitemap | Modern topic sitemap page/count procedures |

See `docs/performance/forum-read-benchmark-2026-06-29.md` for legacy vs modern timing comparisons.

## Private Or Sensitive

| Table | Purpose | Treatment |
| --- | --- | --- |
| `USERS_T` | User accounts and profile fields. | Do not expose wholesale. |
| `USERS_EMAIL_T` | User email records. | Exclude. |
| `Q_PM_T` | Private messages. | Exclude. |
| `Q_BAD_IP_T` | Moderation/security data. | Exclude. |
| `Q_ONLINE_IP_T` | IP tracking. | Exclude. |
| `Q_ONLINE_COUNTRY_T` | Online tracking. | Exclude unless aggregated and reviewed. |
| `MAIL_*` | Mail queue/messages/subscribers. | Exclude from read-only launch. |
| `GENERAL_MAIL_*` | Mail lists/subscribers. | Exclude. |
| `ELMAH_Error` | Error log. | Exclude. |

## Legal Or Security Review Required

| Table | Why |
| --- | --- |
| `Q_TRACKER_PEERS_T` | Torrent tracker data. |
| `Q_TRACKER_TORRENTS_T` | Torrent tracker data. |
| `Q_SHARE_T` | Download/share functionality. |
| `Q_UPLOAD_FILE_T` | Uploaded files. |
| `MP3_T`, `ALT_MP3_T`, `AUDIO_T` | Media/download rights need review. |
| `Q_SCREEN_SAVER_T` | Downloadable files need review. |

