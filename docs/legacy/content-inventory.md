# Content Inventory

This file tracks what content will be brought forward and what needs review.

## Phase 1

### News

Source:

- `NEWS_T`
- `Q_NEWS_FRONT_PAGE_DISPLAY_SP`
- `Q_NEWS_ITEM_DISPLAY_SP`
- `Q_NEWS_VIEW_PAGE_SP`

New routes:

- `/`
- `/news`
- `/news/{id}/{slug}`

Open questions:

- Should `SOURCE_URL` be displayed?
- Should `QUEEN_ONLINE` items be grouped separately?
- Are excerpts trustworthy or should they be generated from article text?

## Phase 2

### Articles

Source:

- `Q_ARTICLE_T`
- `Q_ARTICLE_CATEGORY_T`

New routes:

- `/articles`
- `/articles/category/{id}/{slug}`
- `/articles/{id}/{slug}`

Open questions:

- Check old HTML formatting.
- Confirm whether `SOURCE` is attribution, publication, or URL-like text.

### Biography

Source:

- `Q_BIO_T`

New routes:

- `/biography`
- `/biography/{id}/{slug}`

Open questions:

- Should this be split by band member?
- Is `DISPLAY_SEQUENCE` ascending or descending in intended order?

### FAQ

Source:

- `Q_KNOWLEDGE_BASE_T`
- `Q_KNOWLEDGE_BASE_CATEGORY_T`

New routes:

- `/faq`
- `/faq/category/{id}/{slug}`
- `/faq/{id}/{slug}`

## Phase 3

### Discography

Source:

- `Q_ALBUM_T`
- `Q_ALBUM_SONG_T`
- `Q_ALBUM_REVIEW_T`
- `Q_ARTIST_T`

Important caution:

- `SONG_LYRICS` may involve copyrighted material. Decide whether to publish lyrics before migration.

### Pictures

Source:

- `PIC_FILES_T`
- `PIC_CAT_T`

Need:

- File existence check.
- URL path normalization.
- Blob Storage migration.
- Thumbnail regeneration decision.

### Timeline / Tour / Video

Source:

- `Q_TIMELINE_T`
- `QUEEN_EVENT_T`
- `Q_TOUR_DATE_T`
- `Q_YOUTUBE_T`
- `VIDEO_T`

Need:

- Broken embed check.
- Date normalization.
- Archive wording.

## Later Archive

### Forums

Source:

- `Q_FORUM_T`
- `Q_FORUM_TOPIC_T`
- `Q_FORUM_TOPIC_SUBJECT_T`

Treatment:

- Read-only archive only.
- No posting.
- No login requirement.
- Consider hiding emails, IPs, signatures if inappropriate.
- Consider moderation flags and deleted/hidden posts.

### Blogs

Source:

- `Q_BLOG_T`
- `Q_BLOG_TITLE_T`
- `Q_BLOG_COMMENT_T`

Treatment:

- Later read-only archive.
- Review comments and profile links.

## Excluded By Default

- Private messages.
- User emails.
- Passwords.
- IP tracking.
- Mail subscribers.
- Admin logs.
- Torrent tracker data.
- Unreviewed downloads.

