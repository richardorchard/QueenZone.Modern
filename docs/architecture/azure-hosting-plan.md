# Azure Hosting Plan

## Initial Azure Architecture

```mermaid
flowchart LR
  User["User browser"] --> App["Azure App Service"]
  App --> Sql["Azure SQL Database"]
  App --> Blob["Azure Blob Storage"]
  App --> Insights["Application Insights"]
  GitHub["GitHub Actions"] --> App
```

## Recommended Services

| Concern | Azure Service | Notes |
| --- | --- | --- |
| Web app | Azure App Service | Linux is fine for ASP.NET Core. Windows only needed for legacy Web Forms. |
| Database | Azure SQL Database | Restore or import legacy DB, then connect read-only at first. |
| Media | Azure Blob Storage | Pictures, thumbnails, downloadable public assets. |
| Telemetry | Application Insights | Request tracking, exceptions, dependency timings. |
| Secrets | App Service settings or Key Vault | Start with App Service settings, move to Key Vault if needed. |
| DNS/TLS | App Service custom domain or Azure Front Door | Front Door can wait. |
| CI/CD | GitHub Actions | Build, test, deploy. |

## Environments

Start with:

- Local development.
- Azure preview.
- Production.

Optional later:

- Staging slot for production swaps.

## Configuration

Use configuration keys like:

- `ConnectionStrings:QueenZoneLegacy`
- `Storage:PublicMediaBaseUrl`
- `FeatureFlags:ForumArchiveEnabled`
- `FeatureFlags:LegacyRedirectsEnabled`

## Database Access

For the first release, use a SQL login or managed identity with read-only access wherever possible.

The application should not need write access for Phase 1.

## Deployment Checklist

- Build succeeds in GitHub Actions.
- Tests pass.
- App starts without database write permissions.
- Health endpoint returns OK.
- Application Insights receives requests.
- Old URL redirects are tested.
- No connection strings or secrets are committed.

