# Hosting scale and cache model

Decision for QueenZone production (App Service `queenzone-dev`, plan **ASP-Queenzone**).

## Current production shape (re-verified 2026-08-12)

| Setting | Value |
| --- | --- |
| App Service | `queenzone-dev` |
| Resource group | `Queenzone-RG` |
| App Service plan | `ASP-Queenzone` |
| SKU / tier | **B1 / Basic** (lowest paid plan in use) |
| Worker / instance count | **1** |
| Always On | enabled |
| Redis / Azure CDN / Front Door | **none** |

Full estate inventory for OpenTofu: [`opentofu-inventory.md`](opentofu-inventory.md).

**Explicit product decision:** stay on a **single instance** of the current low-cost plan. Do **not** scale out App Service instances and do **not** add **Azure Cache for Redis** (or similar paid distributed cache) unless budget and traffic later justify revisiting this document.

## Why this is enough for now

Public performance work already relies on **process-local** mechanisms that are correct on one worker:

| Mechanism | Behaviour on single instance |
| --- | --- |
| `IMemoryCache` / `PublicQueryCacheService` | Shared for all requests on the one worker |
| ASP.NET Core output cache (sitemaps + anonymous HTML) | In-process store on the one worker |
| News / sitemap / HTML cache invalidation after admin publish | Bumps local version + evicts local output-cache tags |
| Forum post rate limiter (memory + DB probe) | Counts are consistent for the single process |
| Mobile `/api/v1/auth` (IP + per-member in-process) | Same as website login IP policy, plus a per-account cap on sign-in completion and refresh |
| Per-member daily upload quotas (`MemberUploadQuotaService` / `IMemoryCache`) | Count + byte caps per principal per UTC day on the one worker. Website `/submit/photo` and mobile `POST /api/v1/member/photo-submissions` both call `PhotoSubmissionService.SubmitAsync`, which keys the same `member:{guid}` bucket via `PrincipalKeyFromMemberId`. Web and mobile share one cap because they hit this single worker — not because of Redis, and not via a photo-only counter (forum attachments, editor images, and avatars use the same service). |

Those designs become **incorrect or leaky** only if instance count &gt; 1 (stale HTML/news on another worker, rate-limit bypass, invalidation that does not reach every node).

## Mobile open cost (2026-08-30)

Baseline for epic [#761](https://github.com/richardorchard/QueenZone.Modern/issues/761) / [#764](https://github.com/richardorchard/QueenZone.Modern/issues/764). Measured the GETs `ThreadScreen` and `fetchConversation` already make — not Home, not the whole API. **Cold** = first request after process start. **Warm** = the same URLs immediately after. Host: fresh `Testing` in-memory seed (same sample data CI uses). No Redis. No new cache. No output-cache on private-message GETs.

**Thread open** (signed-in member, busy sample topic `1002` “Ranking every studio album”, 26 posts). `ThreadScreen` loads topic + posts page 1 (`pageSize` 15) + watch when a Bearer token is present + poll when `hasPoll !== false`. Sample topic `1002` leaves `hasPoll` unset, so the poll GET fires and 404s. Page 2 is infinite-scroll only, not on open.

| GET | Status | Cold JSON bytes | Warm JSON bytes | Cache-Control | ETag | Output-cache / Age | PublicQueryCache / existing hit |
| --- | ---: | ---: | ---: | --- | --- | --- | --- |
| `GET /api/v1/forum/topics/1002` | 200 | 227 | 227 | (none) | (none) | miss | miss — not in `PublicQueryCacheService` |
| `GET /api/v1/forum/topics/1002/posts?page=1&pageSize=15` | 200 | 4617 | 4617 | (none) | (none) | miss | miss — not in `PublicQueryCacheService` |
| `GET /api/v1/forum/topics/1002/watch` | 200 | 18 | 18 | `no-store` | (none) | miss | miss — per-member; correctly uncached |
| `GET /api/v1/forum/topics/1002/poll` | 404 | 211 | 211 | (none) | (none) | miss | miss — Problem Details, no poll on this seed |

**Thread request count:** 4 cold, 4 warm. **Thread JSON total:** 5073 bytes both times. Mobile `ContentCache` / `fetchJsonWithOfflineCache` is unused here (news / biography / discography details only).

**Conversation open** (`fetchConversation`, `pageSize` 50, no `page` — server latest window). Seeded 12-message 1:1 thread (Alice / Bob). Message ids and timestamps vary per run; this row is the 2026-08-30 measurement (4094 bytes).

| GET | Status | Cold JSON bytes | Warm JSON bytes | Cache-Control | ETag | Output-cache / Age | PublicQueryCache / existing hit |
| --- | ---: | ---: | ---: | --- | --- | --- | --- |
| `GET /api/v1/me/messages/{conversationId}?pageSize=50` | 200 | 4094 | 4094 | `no-store` | (none) | miss | miss — PM bodies must not enter `PublicQueryCacheService` |

**Conversation request count:** 1 cold, 1 warm.

**N+1 spot-check** (same review bar as ADR 0006): no unbounded or per-row query on these opens. Topic posts are one paged read plus one batched attachment merge. Topic header adds `GetThreadAsync` for lock. Watch and poll each do a topic-exists read plus one lookup (`IsWatching` / `GetPollWithResults`, vote tallies grouped). Conversation is participant + conversation + count + one message page + batched report ids + mark-read + `CanSendReply` / `HasBlocked` — all bounded. No query change in this baseline.

**Versus single-B1 headroom:** one signed-in thread open is four GETs and ~5 KB of JSON; one conversation open is one GET and ~4 KB. Warm repeats miss every process cache (by design for watch/PM; topic/posts are public but not in the query-cache or output-cache sets). That is well inside the current single B1 / no-Redis budget — these opens will not force a hosting-tier or Redis change. Do not add output-cache on authenticated conversation GETs.

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
| [#330](https://github.com/richardorchard/QueenZone.Modern/issues/330) | Per-member daily upload quotas | Process-local; container/size caps still enforced; AV scanning not planned |

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
- [`opentofu-inventory.md`](opentofu-inventory.md) — live estate ownership for OpenTofu  
- [`public-query-cache.md`](public-query-cache.md) — process-local public query cache  
- Epic [#312](https://github.com/richardorchard/QueenZone.Modern/issues/312) — performance / security improvement backlog  
