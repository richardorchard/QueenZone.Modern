# Frontend LCP / media and anonymous HTML cache headers (#333)

Dated note for issue [#333](https://github.com/richardorchard/QueenZone.Modern/issues/333).

## Changes

1. **Hero / design-system images** — `_PictureImage` emits intrinsic `width`/`height` from `DesignSystemImageDimensions` and `fetchpriority="high"` for eager `img-hero` LCP candidates; WebP `<source>` unchanged.
2. **Homepage era LCP** — screenshot uses intrinsic `787×518` (was generic `800×600`).
3. **Anonymous HTML `Cache-Control`** — `public, max-age=60` on successful anonymous public HTML via `PublicHtmlCacheControl` (does not override static/UGC/sitemap headers; excluded admin/account/auth).
4. **Analytics** — gtag loader moved to end of `<body>` and deferred with `requestIdleCallback` (fallback: `load`).

## Measurement workflow

Use the existing script (see `frontend-performance-checks.md`):

```powershell
powershell -File .\scripts\Measure-FrontendPerformance.ps1 -StartLocalApp -FormFactor mobile -Runs 3
```

Compare medians for `/`, `/news`, a news/article detail path, and `/forum` against `frontend-performance-budgets.json`. Paste before/after tables in the PR.

## Post-change lab capture (local sample data)

Command (after change on this branch):

```powershell
powershell -File .\scripts\Measure-FrontendPerformance.ps1 -StartLocalApp -FormFactor mobile -Paths "/,/news,/forum" -Runs 1
```

| Path | Factor | LCP (ms) | CLS | Transfer | Requests | TTFB (ms) | Budget |
| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |
| `/` | mobile | 3092 | 0.00 | 707.7 KB | 18 | 393 | LCP OVER (others ok) |
| `/news` | mobile | 2559 | 0.00 | 261.2 KB | 13 | 58 | LCP OVER (~2%) |
| `/forum` | mobile | 2556 | 0.00 | 256.0 KB | 13 | 29 | LCP OVER (~2%) |

Notes:

- Single local run with simulated mobile throttling; Lighthouse chrome-launcher `EPERM` cleanup noise on Windows (reports still usable). Prefer `-Runs 3` median for PR comparison vs `main`.
- CLS is **0** on all three paths after width/height emission.
- Transfer and request counts sit well under budgets; homepage weight remains hero-dominated as expected.
- LCP slightly over the advisory 2500 ms mobile budget on cold local publish — document, do not treat as a merge gate. Re-check on `queenzone-dev` / production if needed.

## Expected direction

| Area | Expectation |
| --- | --- |
| LCP | Neutral-to-better on hero pages (dimensions + fetchpriority; WebP already present) |
| CLS | Better or equal where design-system images lacked dimensions |
| Transfer / requests | Neutral (no new third-party tags; analytics still one delayed script) |
| TTFB | Neutral; short HTML max-age helps warm browser/CDN only |

Advisory budgets: mobile LCP ≤ 2500 ms, CLS ≤ 0.1, transfer ≤ 1.5 MB, requests ≤ 40.

## Related

- Script: `scripts/Measure-FrontendPerformance.ps1`
- Budgets: `docs/performance/frontend-performance-budgets.json`
- Output cache (server-side, 90s): `PublicOutputCachePolicies`
