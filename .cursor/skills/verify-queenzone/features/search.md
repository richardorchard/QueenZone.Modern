# Search the archive

Search lets a visitor query news, articles, and forum discussions from the header or the dedicated search page, then distinguish matches from no results.

## Sub-features

- `search-open` opens `/search` from the header control and from `/`.
- `search-match` returns the seeded modernisation article for `modernisation`.
- `search-open-result` opens that result into the news article.
- `search-empty` shows the no-results sentence for a query with no matches.
- `search-from-news` submits the news-archive search box and lands on unified search.

## How to get to it (user POV)

- Choose the header control named `Search`.
- Open `/search`.
- Submit the `Search news` box on `/news` (the form posts to `/news/search`, which redirects to `/search`).
- Choose an example tag on the empty search page such as `Freddie Mercury`.

## Driving it with the browser

Preconditions:

- QueenZone is healthy at `http://127.0.0.1:5199`.
- `control-queenzone.ps1 doctor` reports sample article 1003.

- **Header entry.** From `/`, choose the control named `Search`. The URL is `/search`. The level-1 heading `Search` is visible. `#qz-search` is visible and labelled `Search query`.
- **Title match.** Fill `#qz-search` with `modernisation` and choose `Search`. A result card heading `QueenZone modernisation begins` is visible. Filter navigation named `Filter search results by content type` is visible.
- **Open result.** Choose the `QueenZone modernisation begins` card. The article heading `QueenZone modernisation begins` is visible.
- **Empty state.** Return to `/search`, enter `volcano-xyz-no-match`, and choose `Search`. Visible text includes `No results found for` and `volcano-xyz-no-match`.
- **News-archive entry.** Open `/news`, fill the box named `Search news` with `modernisation`, and choose `Search`. After redirect, `#qz-search` contains `modernisation` and the same modernisation result is visible.
- **Proof.** Capture the populated result state to `artifacts/search/results.aria.txt` and `artifacts/search/results.png`. Both identify the query and `QueenZone modernisation begins`.

## Gotchas

- `/news/search` is a compatibility redirect, not a second search UI. After submit, assert `/search?q=...`.
- The empty search page shows example tags. Those are not results. Assert the `No results found for` sentence after a real miss.
- Result cards use `h3` titles inside `a.qz-card`. Prefer the accessible name of the link or the visible heading text, not card position.
- Do not treat `/api/v1` search as proof of this page.
