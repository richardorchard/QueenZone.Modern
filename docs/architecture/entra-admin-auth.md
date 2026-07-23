# Entra admin authentication (App Service)

This documents the Microsoft Entra (Azure AD) app registration and App Service settings used for **admin** sign-in (`/admin/*`). Member social login (`Authentication__*`) is separate.

## Why this exists

PR Phase A production hardening (epic [#312](https://github.com/richardorchard/QueenZone.Modern/issues/312), issues #313–#315) makes the web app **fail to start** outside Development/Testing when `AzureAd:ClientId` is missing or still a placeholder such as `YOUR_CLIENT_ID`.

App Service must therefore carry real Entra settings. Committed `appsettings.json` only has placeholders; secrets never belong in git.

## What was configured (2026-07-23)

| Item | Value / note |
| --- | --- |
| App Service | `queenzone-dev` in resource group `Queenzone-RG` |
| Entra app display name | **QueenZone Admin** |
| Application (client) ID | `f6d32f3b-7a4e-4517-a4d1-0995caad8feb` |
| Sign-in audience | `AzureADandPersonalMicrosoftAccount` (work + personal Microsoft accounts) |
| Tenant setting for the app | `AzureAd__TenantId=common` (matches multi-account audience) |
| Client secret display name | `queenzone-dev-appservice` |
| Client secret created | **2026-07-23** (via `az ad app credential reset --years 2`) |
| **Renew secret by** | **2028-07-01** (allow ~3 weeks before the 2-year expiry; do not wait for outage) |
| ID token issuance | Enabled on the web platform |
| Access control | Entra signs the user in; **admin rights** still require the signed-in email to match `Admin:AllowedEmails` |

### Redirect URIs (web)

- `https://www.queenzone.org/signin-oidc`
- `https://queenzone.org/signin-oidc`
- `https://queenzone-dev.azurewebsites.net/signin-oidc`

Add further hosts here (and in Entra) if you introduce staging slots or new custom domains.

### App Service application settings

Use double-underscore names (ASP.NET Core nested config):

| App setting | Purpose |
| --- | --- |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` |
| `AzureAd__TenantId` | `common` |
| `AzureAd__ClientId` | Application (client) ID above |
| `AzureAd__ClientSecret` | Client secret value (never commit) |
| `AzureAd__CallbackPath` | `/signin-oidc` |

Optional: override allowlist without redeploying:

- `Admin__AllowedEmails__0`, `Admin__AllowedEmails__1`, …

### Related but different app

| Entra app | Used for |
| --- | --- |
| **QueenZone Admin** | Admin OIDC (`Microsoft.Identity.Web` / `/signin-oidc`) |
| **queenzone member login** (`3f4e4a95-7af3-48ce-be28-80d985e4014f`) | Member Microsoft OAuth (`Authentication__Microsoft__*`, `/signin-microsoft`) |

Do not point `AzureAd__*` at the member-login app without also aligning redirect URIs and OIDC vs OAuth schemes.

## Verify current App Service config

Requires `az login` with access to subscription **Base Thinking** / the QueenZone resource group.

```powershell
az webapp config appsettings list `
  --name queenzone-dev `
  --resource-group Queenzone-RG `
  --query "[?starts_with(name, 'AzureAd')].{name:name, length:length(value)}" `
  -o table
```

Expect five `AzureAd__*` rows with non-zero lengths. Do not print secret values into logs, issues, or PRs.

```powershell
az ad app show --id f6d32f3b-7a4e-4517-a4d1-0995caad8feb `
  --query "{displayName:displayName, appId:appId, redirectUris:web.redirectUris, idToken:web.implicitGrantSettings.enableIdTokenIssuance}" `
  -o json
```

Smoke after restart:

```powershell
Invoke-WebRequest -Uri https://www.queenzone.org/health -UseBasicParsing | Select-Object StatusCode
# Then open /admin in a browser and complete Entra sign-in with an allowlisted email.
```

## Client secret renewal (do this before 2028-07-01)

Secrets expire. When admin login starts failing with token/credential errors, or when approaching the renewal date above:

1. Create a **new** secret on the same app (keep the old one until App Service is updated):

   ```powershell
   $appId = "f6d32f3b-7a4e-4517-a4d1-0995caad8feb"
   # Capture stdout only (CLI may write WARNING to stderr)
   $cred = az ad app credential reset --id $appId --append --display-name "queenzone-dev-appservice-$(Get-Date -Format yyyyMMdd)" --years 2 -o json 2>$null | ConvertFrom-Json
   # $cred.password is the new secret — set it next; do not commit it
   ```

2. Update App Service (replace with the new password from step 1):

   ```powershell
   az webapp config appsettings set `
     --name queenzone-dev `
     --resource-group Queenzone-RG `
     --settings "AzureAd__ClientSecret=<new-secret>"
   ```

3. Restart and smoke-test admin login:

   ```powershell
   az webapp restart --name queenzone-dev --resource-group Queenzone-RG
   Invoke-WebRequest -Uri https://www.queenzone.org/health -UseBasicParsing | Select-Object StatusCode
   ```

4. After admin login works, remove the **old** secret in Entra portal (App registration → Certificates & secrets) or via `az ad app credential delete`.

5. Update the **Renew secret by** date in this document (add two years from the new secret’s creation, minus a safety buffer).

### Optional calendar reminder

Set a personal or team calendar reminder for **2028-06-01**: “Rotate QueenZone Admin Entra client secret (see `docs/architecture/entra-admin-auth.md`).”

There is no automated Azure alert in this repo for app-secret expiry; treat this doc + calendar as the control.

## Local development

Local Development may leave `AzureAd:ClientId` empty and use `X-Test-User-Email` with allowlisted emails (see README). Do not copy the production client secret into git-tracked files.

`appsettings.Local.json` is loaded only in Development.

## Failure modes

| Symptom | Likely cause |
| --- | --- |
| App fails to start after deploy | Missing/placeholder `AzureAd__ClientId` on App Service (Phase A fail-closed) |
| Entra login error on redirect | Redirect URI not registered, or wrong ClientId |
| Signed in but 403 on `/admin` | Email not in `Admin:AllowedEmails` (check claim `email` / `preferred_username`) |
| Sudden admin login failure after long uptime | **Expired client secret** — follow renewal steps above |

## Related docs

- `docs/architecture/azure-hosting-plan.md` — hosting overview and hardening summary  
- `docs/agent-handoff-cheatsheet.md` — production debugging shortcuts  
- README — local admin test-header workflow  
