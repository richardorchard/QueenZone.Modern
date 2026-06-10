# SEO And Monetisation Plan

## SEO Position

QueenZone has the kind of archive content that can still perform well in search:

- Biography pages.
- Album pages.
- Song and discography metadata.
- Historical news.
- Picture library.
- FAQ and knowledge base.
- Forum/archive discussions, if published carefully.

The new site should be built for fast, crawlable, canonical, durable pages.

## SEO Requirements

- Static or server-rendered HTML for all important pages.
- Stable canonical URLs.
- Legacy URL redirects.
- XML sitemap split by content type if needed.
- RSS feeds for news.
- Schema.org structured data where appropriate.
- Descriptive titles and meta descriptions.
- Image alt text and dimensions.
- Open Graph and Twitter card metadata.
- Clean internal linking.
- Breadcrumbs for deep archive pages.
- Fast Core Web Vitals.
- No client-side-only content for indexable pages.

## Content Types To Prioritize

### Biography

Likely evergreen search content.

Routes:

- `/biography`
- `/biography/{id}/{slug}`
- Potential future band-member routes.

### Albums

Likely evergreen search content.

Routes:

- `/discography`
- `/discography/albums/{id}/{slug}`
- `/discography/songs/{id}/{slug}`

Important:

- Decide lyrics policy before publishing `SONG_LYRICS`.
- Use album release dates, artwork, track listings, and notes.

### Picture Library

High-value archive content if images are still available.

Routes:

- `/pictures`
- `/pictures/category/{id}/{slug}`
- `/pictures/{id}/{slug}`

Important:

- Preserve image dimensions.
- Generate responsive images.
- Use Blob Storage with stable URLs.
- Add captions/descriptions where available.
- Generate image sitemap if the library is extensive.

## AI Search Reality

Ad-supported informational sites face more uncertainty now because AI answers can reduce clicks for some query types. Recent research on Google AI Overviews has found measurable traffic substitution for some informational content, while other experience-based or community content may behave differently.

Implication for QueenZone:

- Do not rely on display ads as the only success model.
- Build pages that are worth visiting beyond a short factual answer.
- Lean into archive depth, pictures, metadata, community history, and original context.
- Make the site highly citable and structured for search engines and AI systems.

## Monetisation Options

### Display Ads

Still possible, but should be treated as experimental.

Guidelines:

- Do not compromise page speed.
- Do not crowd content.
- Start with limited placements.
- Measure revenue per page type.
- Avoid ads on sensitive memorial/community pages if they feel wrong.

### Affiliate Links

Possible for album pages, books, documentaries, and official merchandise.

Guidelines:

- Clearly disclose affiliate links.
- Prefer tasteful placements.
- Do not let affiliate content dominate archive content.

### Donations / Support

Potentially better aligned with a fan archive.

Options:

- "Support the archive" page.
- GitHub Sponsors, Ko-fi, Patreon, or similar.
- One-off donation link.

### Premium Features

Not recommended for the first release.

Possible later:

- Advanced archive search.
- Curated digital exhibits.
- Community restoration features.

## Measurement Plan

Track:

- Search impressions.
- Search clicks.
- Indexed pages.
- Top landing pages.
- Page speed.
- Ad revenue by page type, if ads are enabled.
- Image search traffic.
- Old URL redirect hits.
- 404s from old links.

Tools:

- Google Search Console.
- Bing Webmaster Tools.
- Application Insights.
- Lightweight privacy-conscious analytics.

## First SEO Milestone

For the first deploy:

- Homepage has complete HTML.
- `/news`, `/biography`, `/discography`, `/pictures` exist or have planned placeholders.
- News detail pages are crawlable.
- Sitemap XML exists.
- Robots.txt exists.
- Old news URLs redirect.
- Page titles and descriptions are unique.

