# Public Query Cache

The public site caches a small set of stable, anonymous query results in ASP.NET Core `IMemoryCache` through `PublicQueryCacheService`.

This is a **process-local** cache. Production runs a **single** App Service worker (B1); multi-instance Redis-backed cache is intentionally **not** used for cost reasons. See [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md).

This cache is intentionally limited to shared public data:

- homepage latest published news
- public news published count
- public article published count
- forum archive category and thread statistics
- homepage "On This Day" and nearby-history snippets

Admin, personalized, authenticated, preview, and edit workflows must not read from this cache.

## Freshness

Default durations are configured by `PublicQueryCacheOptions`:

| Data | Default duration |
| --- | --- |
| Latest news and public news count | 5 minutes |
| Public article count | 30 minutes |
| Forum archive statistics | 30 minutes |
| Homepage history snippets | 12 hours |

Deployments can override these values with the `PublicQueryCache` configuration section. Short TTLs are preferred for editorial data, while forum and history data can tolerate longer staleness because those slices are mostly archive content.

Admin news publish, unpublish, delete, and edits to already-published articles invalidate the public news cache immediately so visitor-facing news pages do not have to wait for TTL expiry after an editorial change.
