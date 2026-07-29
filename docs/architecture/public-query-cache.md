# Public Query Cache

The public site caches a small set of stable, anonymous query results in ASP.NET Core `IMemoryCache` through `PublicQueryCacheService`.

This is a **process-local** cache. Production runs a **single** App Service worker (B1); multi-instance Redis-backed cache is intentionally **not** used for cost reasons. See [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md).

This cache is intentionally limited to shared public data:

- homepage latest published news
- public news published count
- public article published count
- forum archive category and thread statistics
- photography category lists and category pages
- homepage "On This Day" and nearby-history snippets

Admin, personalized, authenticated, preview, and edit workflows must not read from this cache.

## Freshness

Default durations are configured by `PublicQueryCacheOptions`:

| Data | Default duration |
| --- | --- |
| Latest news and public news count | 5 minutes |
| Public article count | 30 minutes |
| Forum archive statistics | 30 minutes |
| Photography categories / category pages | 30 minutes |
| Homepage history snippets | 12 hours |

Deployments can override these values with the `PublicQueryCache` configuration section. Short TTLs are preferred for editorial data, while forum, photo, and history data can tolerate longer staleness because those slices are mostly archive content.

## Invalidation matrix

Keys live in `PublicQueryCacheKeys`. Invalidation APIs live on `PublicQueryCacheService`. Prefer **versioned keys** (bump a version entry) when many key variants exist; prefer **explicit `Remove`** when the key set is fixed and small.

| Cache entry | Key pattern | Default TTL | Invalidation API | Mechanism | Call sites (must stay in sync) | Fallback if missed |
| --- | --- | --- | --- | --- | --- | --- |
| Latest news | `public-query:news:latest:v{version}:{count}` | 5m (`NewsCacheDuration`) | `InvalidateNewsCache` | Bump `public-query:news:version` | Admin news publish / unpublish / delete (`Admin/News/Action`); edit of published news (`Admin/News/EditPost`) | TTL |
| News published count | `public-query:news:published-count:v{version}` | 5m | `InvalidateNewsCache` | Same news version bump | Same as latest news | TTL |
| Article published count | `public-query:articles:published-count` | 30m (`ArticleCountCacheDuration`) | `InvalidateArticleCountCache` | `Remove` fixed key | Admin article actions that change the published set (`Admin/Articles/Action`) | TTL |
| Forum categories | `public-query:forum:categories` | 30m (`ForumStatsCacheDuration`) | `InvalidateForumStatsCache` | `Remove` both forum keys | New thread (`Forum/NewThread`); new post (`Forum/Topic`); some post edits that affect stats (`Forum/EditPost`) | TTL |
| Forum thread count | `public-query:forum:thread-count` | 30m | `InvalidateForumStatsCache` | Same | Same as forum categories | TTL |
| Photo categories | `public-query:photo:categories:v{version}` | 30m (`PhotoCacheDuration`) | `InvalidatePhotoCache` | Bump `public-query:photo:version` | Admin photo writes (`Admin/Photos/*` via `InvalidatePublicPhotoCachesAsync`) | TTL |
| Photo category page | `public-query:photo:category-page:v{version}:{catId}:{page}:{pageSize}` | 30m | `InvalidatePhotoCache` | Same photo version bump | Same as photo categories | TTL |
| On this day | `public-query:history:on-this-day:{yyyyMMdd}:{count}` | 12h (`OnThisDayCacheDuration`) | *(none)* | — | — | TTL only |
| Around this day | `public-query:history:around-this-day:{yyyyMMdd}:{dayWindow}:{count}` | 12h | *(none)* | — | — | TTL only |

Related **output-cache** tags (anonymous HTML / sitemaps) are separate from this query cache. Editorial writes that change public HTML should also `EvictByTagAsync` for `PublicOutputCachePolicies.PublicHtmlTag` / `PublicSitemapTag` as already done from admin news, photo, and sitemap invalidation paths. See `PublicOutputCachePolicies` and issue [#321](https://github.com/richardorchard/QueenZone.Modern/issues/321).

### Contributor rules

1. **Writes that change public aggregates must invalidate.** Admin or member writes that change data exposed through `PublicQueryCacheService` must call the matching invalidate API (or bump the matching versioned key family). Do not rely on TTL alone for editorial news, articles, photos, or forum stats.
2. **New cached queries need a matrix row.** When adding a `GetOrCreateAsync` entry, add a key helper on `PublicQueryCacheKeys`, document the row above, and wire invalidation (or explicitly document TTL-only with rationale, as for queen-history snippets).
3. **Prefer version bumps for open-ended key families.** News latest-count variants and photo category pages use version segments so callers can add new count/page shapes without updating every `Remove` call site.
4. **Single-instance assumption.** Invalidation only affects the current process. Do not design multi-instance consistency on this cache until [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md) allows scale-out and a shared cache product.
5. **Stampede behaviour.** Cold-cache concurrent loads share a process-wide per-key `SemaphoreSlim` gate inside `PublicQueryCacheService` (static `LoadGates`), so scoped service instances still coalesce factory calls. Fixed under [#392](https://github.com/richardorchard/QueenZone.Modern/issues/392); covered by multi-instance stampede tests in `PublicQueryCacheServiceTests`.

### How to verify the matrix against code

```text
# Invalidation APIs and call sites
rg "Invalidate(News|Forum|Article|Photo)|InvalidateArticleCountCache|InvalidateForumStatsCache" src tests

# Key conventions
rg "PublicQueryCacheKeys" src
```

Re-check this document whenever invalidation call sites or key families change.

## Related

- [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md) — single-instance / no Redis decision
- `src/QueenZone.Web/PublicQueryCacheService.cs`
- `src/QueenZone.Web/PublicQueryCacheKeys.cs`
- `src/QueenZone.Web/PublicQueryCacheOptions.cs`
- Output HTML cache policies: `PublicOutputCachePolicies` (Testing disables HTML output cache for deterministic integration tests; Production-shaped hit coverage lives in `PublicOutputCacheTests`)
