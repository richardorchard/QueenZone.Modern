# Homepage

The homepage tells a visitor they are on the restored Queen internet archive, shows latest news, and offers a path into the news list and the site timeline.

## Sub-features

- `home-hero` shows the archive hero heading and the timeline call to action.
- `home-latest-news` shows the Latest news region with a card into `/news`.
- `home-brand` shows the Queenzone.org masthead brand link.

## How to get to it (user POV)

- Open `/` in the browser.
- Choose the Queenzone.org brand mark from any public page.

## Driving it with the browser

Preconditions:

- QueenZone is healthy at `http://127.0.0.1:5199`.
- `control-queenzone.ps1 doctor` reports the Testing sample article.

- **Open home.** Navigate to `/`. The heading `Twenty-five years of the Queen internet zone` is visible. The hero region is named `Homepage hero`.
- **Timeline CTA.** The link `Explore the timeline` points at `/timeline`.
- **Latest news.** Text `Latest news` is visible. The card `a.qz-card[href='/news']` is visible.
- **Brand.** The masthead link `Queenzone.org` is visible.
- **Proof.** Capture the populated home state to `artifacts/homepage/home.aria.txt` and `artifacts/homepage/home.png`. Both identify Queenzone.org, the hero heading, and Latest news.

## Gotchas

- The eyebrow on the hero reads `The Archive`. That is not the page heading.
- `Latest news` is a section header, not an `h1`. Assert the visible text and the `/news` card, not a level-1 heading named Latest news.
- A screenshot of the hero alone is incomplete if the Latest news region is below the fold. Scroll until that region is visible before the proof shot.
