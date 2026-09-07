# GitHub Environments

Issue: [#1377](https://github.com/richardorchard/QueenZone.Modern/issues/1377) (Bob Architecture lock, Option B).

This is the durable map of GitHub Environments used by QueenZone Actions. Environments live only in GitHub Settings today; workflows reference them by exact name. Create and protect an environment **before** merging a workflow that first references it — GitHub auto-creates an unprotected environment on first use.

Do **not** merge workflow PRs that introduce or retarget production environments until `prod-release`, `prod-deploy`, `prod-google-play`, and `prod-data-read` exist with variables/secrets copied and the custom deployment policies below applied.

## Why not rename `dev` / `deploy` in place

GitHub can rename an environment and keep its secrets and variables. That is the wrong move here:

- Legacy `dev` is being **split**. It previously gated production migrate + zip deploy (`deploy.yml`), PR-time production Azure SQL writes (`ci.yml`), production reads that refresh the SQL Express mirror, and vestigial mirror-only probe jobs. One rename cannot express those four trust boundaries.
- Legacy `deploy` is being **split**. It previously gated the production ARM/OIDC identity **and** Google Play signing/upload. Those credentials must not share an environment.

Leave the legacy `dev` and `deploy` environments in Settings until every workflow reference is gone and a tag-based production release has succeeded. Then delete them in a follow-up (see [Legacy environment retirement](#legacy-environment-retirement)).

## Option B: PR-time Azure SQL writes stop

Pre-merge `ci.yml` `ef-migrations` applies pending EF migrations to the **SQL Express mirror** on the Windows self-hosted runner (`localhost\SQLEXPRESS` / `queenzone_legacy_sync`, Integrated Security). It has **no** production connection string and **no** production GitHub Environment.

Production `dotnet ef database update` stays on `deploy.yml` `migrate` (tag `v*` or manual dispatch from `main`), against real Azure SQL, before the zip deploy.

**Tradeoff.** SQL Express is not Azure SQL. Filegroup syntax, collation behaviour, and DTU-bound long DDL can pass on the mirror and fail later on the post-merge migrate. Those failures move later; they do not disappear. A non-production Azure SQL twin (Option C) is a deferred follow-up and must not block #1377.

## Production environments

| Environment | Purpose | Secrets / variables (copy from) | Protection | Workflows / jobs |
| --- | --- | --- | --- | --- |
| `prod-release` | Production migrate + zip deploy | From legacy `dev`: `BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN` (if stored on the environment; repo-level token is also used), `BITWARDEN_APP_SERVICE_DEPLOY_SECRETS`. Bitwarden mapping must yield `AZURE_WEBAPP_PUBLISH_PROFILE`, `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`, and `MOBILE_AUTH_SIGNING_KEY`. | **Custom** branch `main` + tags `v*` | `deploy.yml` `migrate`; `deploy.yml` `deploy` |
| `prod-deploy` | ARM/OIDC App Service settings | From legacy `deploy`: `ARM_CLIENT_ID`, `ARM_TENANT_ID`, `ARM_SUBSCRIPTION_ID`. `configure-app-settings` also needs Bitwarden token + `BITWARDEN_APP_SERVICE_DEPLOY_SECRETS` (for `MOBILE_AUTH_SIGNING_KEY`). | **Custom** branch `main` + tags `v*` | `deploy.yml` `configure-app-settings`; `app-service-setting-names-check.yml` |
| `prod-google-play` | Play signing / store upload | From legacy `deploy` (Play-related only): Bitwarden token, `BITWARDEN_MOBILE_BUILD_SECRETS`, `SENTRY_DSN`, `SENTRY_ORG`, `SENTRY_PROJECT`. Mapping must yield Android keystore outputs, `SENTRY_AUTH_TOKEN`, and `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON`. Do **not** copy ARM OIDC vars here. | **Custom** branch `main` | `publish-android-google-play.yml` `publish-android` only |
| `prod-data-read` | Read production to refresh/resync the SQL Express mirror | From legacy `dev`: Bitwarden token + `BITWARDEN_APP_SERVICE_DEPLOY_SECRETS` so sqlpackage can read `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`. | **Custom** branch `main` (scheduled runs use the default branch) | `nightly-legacy-checks.yml` `sync-legacy-db`; `test-migrations-against-mirror.yml` `resync-mirror` only |

`SIXLABORS_LICENSE_KEY` stays a **repository** secret (workflow `env:`), not an environment secret.

Development App Service environments (`dev-migrate`, `dev-deploy`, `dev-data-refresh`) are unchanged. See [`dev-curated-snapshot.md`](dev-curated-snapshot.md) and [`opentofu-dev-environment.md`](opentofu-dev-environment.md).

## Jobs that must not use a production environment

These jobs touch only the SQL Express mirror, or use no environment secrets/vars:

| Workflow | Job | Why no prod environment |
| --- | --- | --- |
| `ci.yml` | `ef-migrations` | Hard-coded mirror connection string + `Assert-SqlExpressMirrorConnection.ps1`. No Bitwarden, no Azure SQL. |
| `nightly-legacy-checks.yml` | `legacy-read-probes` | Mirror over LAN. Bitwarden is used only for `QUEENZONE_SQL_EXPRESS_PROBE_PASSWORD` via **repository-level** token/mapping. |
| `nightly-legacy-checks.yml` | `legacy-write-probes` | `localhost\SQLEXPRESS` Integrated Security. No environment secrets. |
| `nightly-legacy-checks.yml` | `ui-e2e-realdata` | Windows: Integrated Security. macOS: repository-level Bitwarden probe password. |
| `nightly-legacy-checks.yml` | `residue-check` | Local mirror only. |
| `test-migrations-against-mirror.yml` | `test-migrations` | Local mirror Integrated Security. |

If the probe password later requires an environment-scoped Bitwarden token, create a **non-production** mirror environment. Never attach those jobs to `prod-data-read`.

## OpenTofu (deferred)

GitHub Environments are **not** managed in `infra/` today (`github_repository_environment` is unused). `opentofu-plan` / `opentofu-apply` already exist as environments, so a later import would have a bootstrap ordering problem: those workflows depend on the environments they would manage.

**Decision:** keep Settings/API as the source of truth for this split. Track OpenTofu ownership as a follow-up; do not block #1377 on it. Inventory rows for the new names live in [`opentofu-inventory.md`](opentofu-inventory.md).

## Legacy environment retirement

Do **not** delete legacy `dev` or `deploy` in the #1377 workflow PR.

Post-merge checklist (separate follow-up):

1. Search the default branch: no `environment: dev`, `environment: deploy`, `name: dev`, or `name: deploy` remains under `.github/workflows/` (ignore `dev-migrate` / `dev-deploy` / `dev-data-refresh` and `deploy-dev.yml`).
2. A `v*` production release has succeeded using `prod-release` + `prod-deploy`.
3. `publish-android-google-play.yml` has succeeded on `main` using `prod-google-play` (or is confirmed unused and still remapped).
4. Nightly `sync-legacy-db` and an optional `test-migrations-against-mirror` resync have succeeded using `prod-data-read`.
5. Then delete `dev` and `deploy` in GitHub Settings so a stale workflow cannot silently recreate them.

## Required status check rename

`ci.yml` job display name changed from `EF migrations (Azure SQL)` to `EF migrations (SQL Express mirror)`. If that string is a required check on `main`, update branch protection to the new name when this workflow reaches `main`.
