# Search

Search from Home finds the seeded modernisation article and opens it.

## Sub-features

- `search-open` opens `search-screen` from `home-search`.
- `search-query` types `modernisation` into `search-input`.
- `search-open-1003` opens `search-result-news-1003` onto `news-story-screen`.

## How to get to it (user POV)

- Choose the search control on Home.
- Type a query and choose a result.
- The Archive tab (`archive-hub-screen`) is a different hub; this map uses Home search.

## Driving it with Maestro

Preconditions:

- Contract host is healthy at `http://127.0.0.1:5098`.
- `control-queenzone-mobile.ps1 doctor` reports news 1003.
- The app is installed on a booted emulator.

- **Open and query.** Run `drive -Flow search`. The flow visits Archive once, returns Home, taps `home-search`, types `modernisation`, and waits for `search-result-news-1003`.
- **Open result.** The same flow taps that result and waits for `news-story-screen`.
- **Proof.** Keep the result list and the opened story in `artifacts/search/`.

## Gotchas

- Result ids are derived from `sourceKey`. The stable sample is `search-result-news-1003`.
- Hide the keyboard after typing or the result row may be covered.
- `/search` on the website is a different surface (`verify-queenzone`).
- An empty-state query is not in the committed Maestro flow. Do not invent one mid-run without adding it to `maestro/flows/`.
