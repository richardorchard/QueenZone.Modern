# News Agent Scheduling

Operational guide for running the news discovery worker on a schedule. For pipeline behavior, flags, and local secrets, see `news-agent.md`.

## Worker entry point

Publish or run the worker project:

```powershell
dotnet publish src/QueenZone.NewsAgent.Worker/QueenZone.NewsAgent.Worker.csproj -c Release -o ./publish/news-agent
```

```powershell
# Full scheduled pipeline: seed sources, fetch, triage, draft
dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news --scheduled

# Equivalent explicit flags
dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news --seed-sources --triage --draft
```

| Flag | Purpose |
|------|---------|
| `--scheduled` | Preset for automation: `--seed-sources --triage --draft` |
| `--fetch-only` | Fetch feeds only (no AI) |
| `--force` | Bypass source poll-interval skip **and** the run lease (manual overlap override) |

Exit codes:

- `0` — success, or run skipped because another instance holds the lease
- `1` — one or more source/AI step failures (Task Scheduler / Azure can treat this as a failed run)

## Overlap protection

When `NewsAgentScheduler:UseRunLease` is `true` (default), each `discover-news` run acquires a database lease before processing. A second concurrent run exits `0` immediately with a log line such as:

```text
Skipping discover-news run because lease discover-news is held by another instance.
```

Configuration (`appsettings.json` or Azure App Settings):

```json
{
  "NewsAgentScheduler": {
    "UseRunLease": true,
    "LeaseName": "discover-news",
    "LeaseDurationMinutes": 120
  }
}
```

- Set `LeaseDurationMinutes` longer than your longest expected run.
- Use `--force` only for deliberate manual reruns while a scheduled job is still active.
- In-memory/sample mode (no `ConnectionStrings:QueenZoneLegacy`) uses an in-process lease store suitable for local dev, not cross-machine coordination.

Apply migrations before the first leased run against a real database:

```powershell
dotnet ef database update --project src/QueenZone.Data --startup-project src/QueenZone.Web
```

## Windows Task Scheduler (local pilot)

Use `scripts/Run-NewsAgentDiscovery.ps1` as the scheduled action. It runs from the repository root, forwards exit codes, and supports common modes.

### One-time setup

1. Copy `src/QueenZone.NewsAgent.Worker/appsettings.Local.json.example` to `appsettings.Local.json` and set `ConnectionStrings:QueenZoneLegacy` and `OpenRouter:ApiKey`.
2. Ensure the database has discovery migrations applied.
3. Smoke-test manually: `scripts/Smoke-NewsAgent.bat` or `scripts/Run-NewsAgentDiscovery.ps1 -Scheduled`.

### Create the task

1. Open **Task Scheduler** → **Create Task**.
2. **General**: run whether user is logged on or not; use an account that can reach SQL Server and the network.
3. **Triggers**: e.g. daily at 06:00, or every 4 hours for busier sources.
4. **Actions** → **Start a program**:
   - **Program**: `powershell.exe`
   - **Arguments**: `-NoProfile -ExecutionPolicy Bypass -File "C:\path\to\QueenZone.Modern\scripts\Run-NewsAgentDiscovery.ps1" -Scheduled`
   - **Start in**: `C:\path\to\QueenZone.Modern`
5. **Settings**: stop task if it runs longer than 2 hours; if the task is already running, **Do not start a new instance** (belt-and-braces with the DB lease).

Optional log file (ignored path, not committed):

```powershell
scripts/Run-NewsAgentDiscovery.ps1 -Scheduled *>&1 | Tee-Object -FilePath "$env:LOCALAPPDATA\QueenZone\news-agent.log" -Append
```

## Azure hosting options

The web app deploy workflow (`deploy-app-service.yml`) publishes `QueenZone.Web` only. The news agent worker is a **separate** console app. Pick one of the paths below when you need cloud automation.

### Option A — Azure Container Apps Job (recommended for isolated scheduling)

1. `dotnet publish src/QueenZone.NewsAgent.Worker -c Release -o publish/news-agent`
2. Containerize the publish output (Dockerfile calling `dotnet QueenZone.NewsAgent.Worker.dll discover-news --scheduled`).
3. Deploy a Container Apps **Job** with a cron trigger (e.g. `0 6 * * *`).
4. Set App Settings / secrets:
   - `ConnectionStrings__QueenZoneLegacy`
   - `OPENROUTER_API_KEY` or `OpenRouter__ApiKey`
5. Use the same SQL database as the web app so `/admin/news-discovery` sees new candidates.

### Option B — Azure Functions timer trigger

1. Add a timer-triggered function project that references `QueenZone.NewsAgent` and `QueenZone.Data`.
2. On timer fire, build a DI container mirroring `QueenZone.NewsAgent.Worker/Program.cs` and call `DiscoverNewsWorker.RunAsync` with `--scheduled` options.
3. Configure connection string and OpenRouter key in Function app settings.
4. Keep function timeout above worst-case fetch + triage + draft duration.

### Option C — App Service WebJob

1. Publish the worker to a folder and zip as a **triggered WebJob** under `App_Data/jobs/triggered/news-discovery`.
2. Add a `settings.job` CRON schedule, e.g. `{ "schedule": "0 0 6 * * *" }`.
3. Set the same connection string and OpenRouter secrets on the App Service.
4. Do **not** host the worker inside the scaled-out web process without the DB lease; overlapping instances will otherwise duplicate work.

### Option D — Operator machine (current default)

Run `scripts/Run-NewsAgentDiscovery.ps1 -Scheduled` via Task Scheduler on an always-on Windows PC. Discovery pauses when the machine is off; the public site is unaffected.

## Secrets

Never commit API keys or connection strings. Use:

- Local: `src/QueenZone.NewsAgent.Worker/appsettings.Local.json` (git-ignored)
- Azure: App Service / Function / Container App configuration
- CI: not required for default tests (fakes only)

## Related

- `docs/architecture/news-agent.md` — pipeline, admin review, smoke test
- `docs/backlog/news-agent-mvp-handoff.md` — MVP scope and GitHub issues (#107)
- `scripts/Smoke-NewsAgent.bat` — manual OpenRouter smoke (fetch + triage)
