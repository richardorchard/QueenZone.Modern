# Old URL Map

Old URL preservation is important for SEO, fan-site backlinks, forum links, and archive quality.

This is a starter map. Validate against legacy `web.config`, IIS rewrite rules, sitemap, and real traffic if logs are available.

## High Priority Redirects

| Old URL | New URL | Notes |
| --- | --- | --- |
| `/` | `/` | Homepage. |
| `/default.aspx` | `/` | Legacy homepage. |
| `/news.aspx` | `/news` | News archive. |
| `/process/news_view.aspx?news_id={id}` | `/news/{id}/{slug}` | News detail. |
| `/articles.aspx` | `/articles` | Articles archive. |
| `/article_category.aspx?q={id}` | `/articles/category/{id}/{slug}` | Confirm query parameter. |
| `/process/article_show.aspx?q={id}` | `/articles/{id}/{slug}` | Article detail. |
| `/biography/default.aspx` | `/biography` | Biography landing. |
| `/process/bio_view.aspx?q={id}` | `/biography/{id}/{slug}` | Biography detail. |
| `/faq.aspx` | `/faq` | FAQ landing. |
| `/faq_category_view.aspx?q={id}` | `/faq/category/{id}/{slug}` | FAQ category. |
| `/process/faq_view.aspx?q={id}` | `/faq/{id}/{slug}` | FAQ detail. |
| `/pictures.aspx` | `/pictures` | Picture landing. |
| `/picture-category.aspx?q={id}` | `/pictures/category/{id}/{slug}` | Picture category. |
| `/process/picture_view.aspx?Q={id}` | `/pictures/{id}/{slug}` | Picture detail. |
| `/discography/album_view.aspx?q={id}` | `/discography/albums/{id}/{slug}` | Album detail. |
| `/discography/song_view.aspx?q={id}` | `/discography/songs/{id}/{slug}` | Song detail. |
| `/forums/` | `/archive/forums` | Forum archive landing. |
| `/forums/default.aspx` | `/archive/forums` | Forum archive landing. |
| `/forums/forum_view.aspx?q={id}` | `/archive/forums/{id}/{slug}` | Forum category. |
| `/forums/forum_topic_view.aspx?q={id}` | `/archive/forums/topics/{id}/{slug}` | Forum topic. |
| `/profile.aspx?q={id}` | `/archive/members/{id}` | Only if member archive is enabled. |

## Redirect Implementation Notes

- Prefer permanent redirects only after validating content exists.
- Use temporary redirects in preview while testing.
- Preserve query-string IDs.
- Generate slugs from titles but keep legacy IDs canonical.
- For missing/unsafe content, redirect to a parent archive page or return 404/410 deliberately.

