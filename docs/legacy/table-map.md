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
| `Q_FORUM_T` | Forum categories. | Read-only archive (categories wired on `/forum`). |
| `Q_FORUM_TOPIC_T` | Forum topics and replies. | Later read-only archive. |
| `Q_FORUM_TOPIC_SUBJECT_T` | Forum subject metadata. | Later read-only archive. |

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
| `dbUser.Q_FORUM_TOPIC_THREAD_COUNT_V` | Thread count per forum (`Q_FORUM_TOPIC_PARENT_ID = 0`). |
| `FORUM_VIEW_V` | Forum topics joined to users (used by `Q_FORUM_VIEW_SP`). |
| `Q_BLOG_T` | Blog posts. | Later archive. |
| `Q_BLOG_TITLE_T` | Blog ownership/title metadata. | Later archive. |
| `Q_BLOG_COMMENT_T` | Blog comments. | Later archive, privacy/moderation review. |
| `Q_COMMENT_T` | Generic comments. | Later review. |
| `Q_GENERAL_COMMENT_T` | Generic comments. | Later review. |

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

