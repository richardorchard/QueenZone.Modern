# Queen Links Check Scheduling

The public `/links` page renders legacy `QUEEN_FEATURED_SITE_T` links, grouped by `Q_LINK_CAT_T`.
It does not probe external sites during page rendering. Link availability is checked by a scheduled
tools command that persists status in `QueenLinkChecks`.

## Command

```powershell
powershell -File .\scripts\Check-QueenLinks.ps1
```

For a double-clickable/local Task Scheduler wrapper:

```bat
scripts\Check-QueenLinks.bat
```

The script wraps:

```powershell
dotnet run --project .\src\QueenZone.Tools\QueenZone.Tools.csproj -- check-links
```

Connection string resolution:

1. `-ConnectionString`
2. `ConnectionStrings__QueenZoneLegacy`
3. `src/QueenZone.Web/appsettings.Local.json` `ConnectionStrings:QueenZoneLegacy`
4. `src/QueenZone.Web/appsettings.Local.json` `ConnectionStrings:QueenZoneLegacyLive`

Useful options:

```powershell
powershell -File .\scripts\Check-QueenLinks.ps1 -Concurrency 8 -ConfirmAfter 2 -TimeoutSeconds 10
powershell -File .\scripts\Check-QueenLinks.ps1 -DryRun
```

## Visibility Rules

The checker stores `LastCheckedAtUtc`, `LastStatusCode`, `LastError`, `ConsecutiveFailureCount`,
`IsAvailable`, and `IsConfirmedDead`.

The public page hides only links with `IsConfirmedDead = true`.

Hard failures include invalid URLs, DNS/connectivity failures, HTTP `400`, `404`, `410`, and `5xx`.
Temporary timeouts are recorded but do not confirm a link as dead. Successful checks reset the failure
count and make a previously hidden link visible again.

Default behavior confirms a link as dead after two consecutive hard failures.

## Suggested Schedule

Weekly is enough for archive links. Daily is also safe at the current link volume, but avoid running
more often than needed because the task probes third-party fan sites.
