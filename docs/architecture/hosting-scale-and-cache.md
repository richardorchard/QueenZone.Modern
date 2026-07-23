# Hosting scale and cache model

Decision for QueenZone production (App Service `queenzone-dev`, plan **ASP-Queenzone**).

## Current production shape (verified 2026-07-23)

| Setting | Value |
| --- | --- |
| App Service | `queenzone-dev` |
| Resource group | `Queenzone-RG` |
| App Service plan | `ASP-Queenzone` |
| SKU / tier | **B1 / Basic** (lowest paid plan in use) |
| Worker / instance count | **1** |
| Always On | enabled |

**Explicit product decision:** stay on a **single instance** of the current low-cost plan. Do **not** scale out App Service instances and do **not** add **Azure Cache for Redis** (or similar paid distributed cache) unless budget and traffic later justify revisiting this document.

## Why this is enough for now

Public performance work already relies on **process-local** mechanisms that are correct on one worker:

| Mechanism | Behaviour on single instance |
| --- | --- |
| `IMemoryCache` / `PublicQueryCacheService` | Shared for all requests on the one worker |
| ASP.NET Core output cache (sitemaps + anonymous HTML) | In-process store on the one worker |
| News / sitemap / HTML cache invalidation after admin publish | Bumps local version + evicts local output-cache tags |
| Forum post rate limiter (memory + DB probe) | Counts are consistent for the single process |

Those designs become **incorrect or leaky** only if instance count &gt; 1 (stale HTML/news on another worker, rate-limit bypass, invalidation that does not reach every node).

## Archived / deferred work (cost)

Tracked under epic [#312](https://github.com/richardorchard/QueenZone.Modern/issues/312) Phase D:

| Issue | Title | Disposition |
| --- | --- | --- |
| [#323](https://github.com/richardorchard/QueenZone.Modern/issues/323) | Distributed cache/rate-limits (Redis) | **Not planned** while on single B1 — closed as not planned |
| [#326](https://github.com/richardorchard/QueenZone.Modern/issues/326) | Document scale-out readiness | **Done** by this document |

Do not reopen #323 unless this document is updated to allow multi-instance hosting **and** a paid distributed cache (or an accepted alternative).

## Still in scope without Redis or a larger plan

These improve reliability on the current B1 single worker and do **not** require scale-out:

| Issue | Title | Notes |
| --- | --- | --- |
| [#324](https://github.com/richardorchard/QueenZone.Modern/issues/324) | Azure SQL retry + sane command timeouts | Transient fault handling; no new Azure SKU |
| [#325](https://github.com/richardorchard/QueenZone.Modern/issues/325) | Readiness health checks (SQL/blob) | Ops signal only; keep `/health` cheap for liveness |

## If scale-out is reconsidered later

Before raising instance count above 1:

1. **Budget** — confirm willingness to pay for either sticky sessions alone (still weak for cache invalidation) or, preferably, **Azure Cache for Redis** (or equivalent) for:
   - distributed `IMemoryCache` / public query cache  
   - output-cache store  
   - rate-limit counters (or keep rate limits DB-backed only)
2. **Invalidation** — editorial publish must reach every node (Redis key version, pub/sub, or shared output-cache tag store).
3. **Update this document** — record new instance count, SKU, and cache product.
4. **Reopen or replace #323** with a concrete design and cost note.

Until then, **assume single instance** in all performance and caching designs.

## Related docs

- [`azure-hosting-plan.md`](azure-hosting-plan.md) — overall Azure shape  
- [`public-query-cache.md`](public-query-cache.md) — process-local public query cache  
- Epic [#312](https://github.com/richardorchard/QueenZone.Modern/issues/312) — performance / security improvement backlog  
