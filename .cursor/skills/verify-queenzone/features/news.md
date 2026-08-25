# News archive

News lets a visitor browse the chronological archive, move between pages, and read a published article including its body and crest image.

## Sub-features

- `news-index` shows the News heading and at least one row linking into an article.
- `news-page-2` moves from page 1 to `/news/page/2` with a different first title.
- `news-detail-1003` opens the seeded modernisation article and shows its body.
- `news-hidden-draft` keeps `Hidden moderation draft` off the public list.

## How to get to it (user POV)

- Open `/news`.
- Choose the `/news` card from the homepage Latest news region.
- Open `/news/1003/queenzone-modernisation-begins` from a search result or a list row.
- Use the archive pagination Next control on `/news`.

## Driving it with the browser

Preconditions:

- QueenZone is healthy at `http://127.0.0.1:5199`.
- `control-queenzone.ps1 doctor` reports sample article 1003.

- **Open archive.** Navigate to `/news`. The level-1 heading `News` is visible. At least one `.qz-news-row a` link is visible. Page summary text contains `Page 1`.
- **Hidden draft stays hidden.** The page text does not contain `Hidden moderation draft`.
- **Open article 1003.** Navigate to `/news/1003/queenzone-modernisation-begins` or choose that title from the list. The level-1 heading `QueenZone modernisation begins` is visible. `article.article-body` contains `ASP.NET Core` and `news archive`. An image named `QueenZone crest` is visible.
- **Paginate.** Return to `/news`. Record the first `.qz-news-row a` title. Choose `a.archive-pagination-next`. The URL ends with `/news/page/2`. Summary text contains `Page 2`. The first row title is different from page 1.
- **Proof.** Capture the article state to `artifacts/news/detail.aria.txt` and `artifacts/news/detail.png`. Both identify the heading `QueenZone modernisation begins` and the article body.

## Gotchas

- `/news/search` immediately redirects to `/search`. Searching from the news page is covered in `search.md`, not here.
- Article 9001 is a hidden seed. If it appears on `/news`, the host is not a valid public Testing surface.
- Pagination proof needs both the URL change and a different first title. A URL-only check is not enough.
- The canonical link for article 1003 must contain `/news/1003/queenzone-modernisation-begins`.
- The seeded crest image at `/ugc/news/sample-crest.jpg` may 404. The `QueenZone crest` accessible name is still the proof; a broken-image placeholder is not a host failure.
