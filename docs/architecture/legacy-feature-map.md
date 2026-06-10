# Legacy Feature Map

This map identifies the legacy domains and how they should be treated in the new read-only-first rebuild.

| Legacy Feature | Main Tables | Main Procedures | Initial Status | Notes |
| --- | --- | --- | --- | --- |
| News | `NEWS_T` | `Q_NEWS_FRONT_PAGE_DISPLAY_SP`, `Q_NEWS_ITEM_DISPLAY_SP`, `Q_NEWS_VIEW_PAGE_SP`, `Q_NEWS_LIST_ALL_SP` | Phase 1 | Best first vertical slice. |
| Articles | `Q_ARTICLE_T`, `Q_ARTICLE_CATEGORY_T` | `Q_ARTICLE_FRONT_SP`, `Q_ARTICLE_DISPLAY_SP`, `Q_ARTICLE_LIST_BY_CATEGORY_SP` | Phase 2 | Straightforward public content. |
| Biography | `Q_BIO_T` | `Q_BIO_LIST_SP`, `Q_BIO_DISPLAY_SP` | Phase 2 | Good static-ish content area. |
| Discography | `Q_ALBUM_T`, `Q_ALBUM_SONG_T`, `Q_ALBUM_REVIEW_T`, `Q_ALBUM_RATING_T`, `Q_ARTIST_T` | `Q_ALBUM_T_DISPLAY_SP`, `Q_ALBUM_T_LIST_SP`, `Q_ALBUM_SONG_T_DISPLAY_SP`, `Q_ALBUM_SONG_T_LIST_SP` | Phase 2 | Structured content, likely worth redesigning carefully. |
| Pictures | `PIC_FILES_T`, `PIC_CAT_T`, `Q_RANDOM_PIC_T`, `Q_PIC_TAG_T` | `Q_PICTURES_FRONT_SP`, `Q_PIC_DISPLAY_SP`, `Q_PIC_CAT_PAGE_SP`, `Q_PIC_LIST_FILES_SP` | Phase 3 | Needs asset-path audit and Blob Storage plan. |
| FAQ | `Q_KNOWLEDGE_BASE_T`, `Q_KNOWLEDGE_BASE_CATEGORY_T` | `Q_FAQ_LIST_SP`, `Q_KNOWLEDGE_BASE_DISPLAY_SP`, `Q_KNOWLEDGE_BASE_QUESTIONS_BY_CATEGORY_SP` | Phase 2 | Good public content. |
| Links | `Q_LINK_CAT_T`, likely generated/link tables | `Q_LINK_CAT_LIST_SP`, `Q_LINK_CATEGORY_LIST_SP`, `Q_LINKS_ALL_LIST_SP` | Phase 2 | Validate live data before publishing. |
| Quotes | `QUEEN_QUOTE_T` | `QUEEN_QUOTE_T_DISPLAY_SP`, `QUEEN_QUOTE_T_LIST_SP`, `Q_QUOTE_FRONT_PAGE_DISPLAY_SP` | Phase 2 | Good homepage/sidebar content. |
| Featured Sites | `QUEEN_FEATURED_SITE_T` | `QUEEN_FEATURED_SITE_T_DISPLAY_SP`, `QUEEN_FEATURED_SITE_T_LIST_SP`, `Q_FEATURED_SITE_FRONT_PAGE_SP` | Phase 2 | Check links for rot. |
| Timeline | `Q_TIMELINE_T`, `QUEEN_EVENT_T` | `Q_TIMELINE_T_DISPLAY_SP`, `Q_TIMELINE_T_LIST_SP` | Phase 3 | Good candidate for a rich modern page. |
| Tour Dates | `Q_TOUR_DATE_T` | `Q_TOUR_DATE_DISPLAY_SP`, `Q_TOUR_DATE_LIST_SP` | Phase 3 | Likely archival. |
| YouTube/Video | `Q_YOUTUBE_T`, `Q_YOUTUBE_SUBMISSION_T`, `VIDEO_T` | `Q_YOUTUBE_T_DISPLAY_SP`, `Q_YOUTUBE_T_LIST_SP`, `Q_VIDEO_RANDOM_SP`, `Q_VIDEOS_BY_ALBUM_SP` | Phase 3 | Check broken embeds and copyright context. |
| Blogs | `Q_BLOG_T`, `Q_BLOG_TITLE_T`, `Q_BLOG_COMMENT_T`, tags/thumbs | Many `Q_BLOG_*` procs | Later archive | Contains user content and comments. Treat cautiously. |
| Forums | `Q_FORUM_T`, `Q_FORUM_TOPIC_T`, `Q_FORUM_TOPIC_SUBJECT_T` | `Q_FORUM_VIEW_SP`, `Q_FORUM_TOPIC_VIEW_SP`, `Q_FORUM_SEARCH_SP` | Later read-only archive | High preservation value, high moderation/privacy risk. |
| Users | `USERS_T`, `USERS_EMAIL_T`, profile tables | `Q_USER_*`, `USERS_*`, `USER_*` | Do not migrate initially | Avoid exposing private fields. |
| Private Messages | `Q_PM_T` | `Q_PM_*` | Exclude | Private data. Do not publish. |
| Uploads/Attachments | `Q_UPLOAD_FILE_T`, forum/blog attachment tables | `sp_Q_ATTACH_ADD_SP`, attachment/download procedures | Later audit | Needs file inventory and security review. |
| Polls/Quiz | `Q_POLL_*`, `Q_QUIZ_*` | `Q_POLL_*`, `Q_QUIZ_*` | Later optional | Could be archived or rebuilt. |
| Tracker/Torrents | `Q_TRACKER_*`, `Q_SHARE_T` | `Q_TRACKER_*`, `Q_SHARE_*` | Exclude initially | Legal/security risk. |
| Mail/Newsletter | `MAIL_*`, `GENERAL_MAIL_*`, subscribers | `Q_MAILING_LIST_*` | Rebuild if needed | Do not migrate subscriber operations blindly. |

