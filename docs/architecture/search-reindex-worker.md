# Search Reindex Worker

Operational guide for the standalone worker that keeps the unified `/search` `SearchDocument` index fresh in production (issue #527). Modeled on `src/QueenZone.NewsAgent.Worker` — see `docs/architecture/news-agent-scheduling.md` for the pattern this mirrors.

## Why a separate worker

`SearchReindexBuilder.ReindexAllAsync()` (`src/QueenZone.Web/Search/SearchReindexBuilder.cs`) rebuilds `SearchDocument` rows for every indexed content type. Two other mechanisms already keep search partially fresh:

- Best-effort immediate upserts on News/Community-Article admin publish/unpublish/delete (swallow-and-log on failure — the scheduled reindex here is their correctness backstop).
- A manual, in-process, non-durable admin-triggered job (`SearchReindexJobService`, `/admin/search`) — fine for an operator-initiated rebuild, but not durable across app restarts and not automatic.

Content with no immediate-hook trigger at all (legacy articles, biography, discography, timeline, fan performances) relies entirely on a batch job actually running. Nothing did that against the real SQL Server-backed environment until this worker.

## Worker entry point

```powershell
# Editorial-safe scheduled run: lease-protected, records a resumable request row
dotnet run --project src/QueenZone.SearchReindex.Worker -- reindex --scheduled

# Manual override, bypassing the run lease
dotnet run --project src/QueenZone.SearchReindex.Worker -- reindex --force
```

| Flag | Purpose |
|------|---------|
| `--scheduled` | No-op marker for automation call sites; behavior is the same as no flag |
| `--force` | Bypass the run lease (manual overlap override) |

Exit codes:

- `0` — success, or run skipped (lease held elsewhere, or the queued request wasn't claimable this pass)
- `1` — `SearchReindexBuilder.ReindexAllAsync()` threw

## Overlap protection and resumability

When `SearchReindexScheduler:UseRunLease` is `true` (default), each run acquires a `SearchReindexLeases` database lease before processing — a concurrent run exits `0` immediately:

```text
Skipping search-reindex run because lease search-reindex is held by another instance.
```

Under the lease, the run queues a `SearchReindexRunRequests` row (single-flight, deduplicated like the News run-request queue) and claims it before calling `ReindexAllAsync()`, then marks it `Completed`/`Failed`. If a run crashes mid-flight without releasing the lease, `ClaimNextAsync` reclaims `Running` rows stuck past a 3-hour stale timeout on the next successful acquire, so the next scheduled trigger retries rather than leaving the request stranded.

Configuration (`appsettings.json` or Azure App Settings):

```json
{
  "SearchReindexScheduler": {
    "UseRunLease": true,
    "LeaseName": "search-reindex",
    "LeaseDurationMinutes": 60
  }
}
```

Apply migrations before the first leased run against a real database:

```powershell
dotnet ef database update --project src/QueenZone.Data --startup-project src/QueenZone.Web
```

## Windows Task Scheduler (local pilot)

Same pattern as the news agent (`docs/architecture/news-agent-scheduling.md`, Option D — the current default there too):

1. Copy `src/QueenZone.SearchReindex.Worker/appsettings.Local.json.example` to `appsettings.Local.json` and set `ConnectionStrings:QueenZoneLegacy`.
2. Smoke-test manually: `scripts/Run-SearchReindexWorker.ps1 -Scheduled`.
3. **Task Scheduler** → **Create Task** → **Triggers**: daily, repeating every 6 hours. **Actions**: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\path\to\QueenZone.Modern\scripts\Run-SearchReindexWorker.ps1" -Scheduled`, **Start in** the repo root. **Settings**: if already running, **Do not start a new instance** (belt-and-braces with the DB lease).

## Cloud hosting options

Not committed as code in this repo — same four options documented for the news agent apply directly (`docs/architecture/news-agent-scheduling.md` "Azure hosting options"): Azure Container Apps Job with a cron trigger, Azure Functions timer trigger, App Service WebJob with a `settings.job` CRON schedule, or the operator-machine Task Scheduler pilot above (current default in practice).

## Not blocking / out of scope for #527

Wiring the News/Community-Article admin publish/unpublish hook failure paths to queue a `SearchReindexRunRequests` row (instead of only logging a warning) is deliberately not done here — those immediate hooks remain the fast path, and this worker is the scheduled correctness backstop for content that has no hook at all. If that hook-failure retry path is wanted later, `ISearchReindexRunRequestRepository.QueueAsync` already supports queuing an ad-hoc request; it would need a claim-and-run path in this worker (or a second `process-reindex-requests` command, mirroring `QueenZone.NewsAgent.Worker`'s `process-news-requests`) to consume it.

## Secrets

Never commit API keys or connection strings. Use:

- Local: `src/QueenZone.SearchReindex.Worker/appsettings.Local.json` (git-ignored)
- Azure: App Service / Function / Container App configuration
- CI: not required for default tests (fakes only)

## Related

- `docs/architecture/news-agent-scheduling.md` — the pattern this worker mirrors
- `src/QueenZone.Web/Search/SearchReindexBuilder.cs` — what actually gets reindexed
