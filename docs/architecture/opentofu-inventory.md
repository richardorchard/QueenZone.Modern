# OpenTofu live-estate inventory and ownership boundaries

Issue: [#624](https://github.com/richardorchard/QueenZone.Modern/issues/624) (OpenTofu 1/8 under epic [#615](https://github.com/richardorchard/QueenZone.Modern/issues/615)).

**Audit date:** 2026-08-12  
**Method:** read-only Azure CLI/`az` against subscription `Base Subscription`, live HTTP/DNS probes of public hostnames, Cloudflare API (token `CLOUDFLARE_API_TOKEN_READONLY` from Bitwarden — value not recorded), GitHub API for environments/secret *names*, Bitwarden Secrets Manager key *names* only.  
**Mutations performed:** none.

Sanitised machine-readable IDs and import hints live under [`infra/import/`](../../infra/import/).

## Scope boundaries

| In scope for OpenTofu adoption | Out of scope / never OpenTofu |
| --- | --- |
| Azure resources in `Queenzone-RG` | Application schema/data (EF migrations, SQL content) |
| Cloudflare zone DNS, TLS mode, Workers/routes for QueenZone hostnames | DNS registrar ownership / domain purchase |
| App Service hostname bindings, access restrictions, plan SKU | Secret *values* (connection strings, publish profiles, OAuth secrets, OpenRouter keys) |
| Storage account settings, container public-access flags, soft-delete | Blob object contents |
| Application Insights + Log Analytics caps/retention, alerts | GitHub Actions workflow *logic* (stays in `.github/workflows`) |
| Declaring *which* App Service setting *names* exist | Putting secret values into OpenTofu state or `.tfvars` |

Other Azure resource groups on the same subscription (`RichardOrchardRG`, `RO-Backup`, Cloud Shell, default Insights groups, etc.) are **personal/shared operator estate** — keep outside the QueenZone OpenTofu stack unless deliberately expanded later.

## Verified SKU / scale constraints

Live state matches the product decision in [`hosting-scale-and-cache.md`](hosting-scale-and-cache.md):

| Item | Live value |
| --- | --- |
| App Service plan | `ASP-Queenzone`, **B1 / Basic**, capacity **1**, Linux |
| App Service | `queenzone-dev`, Always On **on**, workers **1**, no deployment slots |
| Redis / Front Door / Azure CDN | **None** in `Queenzone-RG` or subscription QueenZone resources |
| Azure SQL | `queenzone-db` on Basic (5 DTU), max size 2 GB, LRS short-term backup |
| Storage | `queenzone`, Standard_LRS, Hot |

Do not encode scale-out, Redis, Front Door, or multi-instance assumptions in OpenTofu modules.

## `cdn` vs `cdn2` — live resolution

Repository docs disagreed. Live behaviour (2026-08-12):

| Hostname | Live routing | Evidence | Correct doc stance |
| --- | --- | --- | --- |
| `cdn.queenzone.org` | **Straight Cloudflare proxy** to Azure Blob. **No Worker header rewriting.** | Successful photo/CSS responses pass through Azure `x-ms-*` headers; Cloudflare `Cache-Control: max-age=14400`; **no** Worker-added `Access-Control-Allow-Origin` / `X-Content-Type-Options`. Azure Storage **custom domain** is registered as `cdn.queenzone.org`. | Matches `AGENTS.md`, `blob-storage-ugc.md`, `picture-library-plan.md`, `PhotoImageUrl.cs`. |
| `cdn2.queenzone.org` | **Cloudflare Worker** `pictures-queenzone-org` on route `cdn2.queenzone.org/*`, fetching `https://queenzone.blob.core.windows.net`. | API route + script inventory; live responses add `Access-Control-Allow-Origin: *`, `X-Content-Type-Options: nosniff`, `Cache-Control: public, max-age=86400, s-maxage=2592000`. Script does **not** currently set `Content-Disposition`. No Azure custom domain for `cdn2`. | Matches `SongFileUrl.cs` / forum attachment redirect target. Worker *name* is historical (“pictures”) but route is **cdn2 only**. |

`docs/architecture/azure-hosting-plan.md` previously attributed Worker `pictures-queenzone-org` and route `cdn.queenzone.org/*` to **cdn**, and told operators not to add an Azure Storage custom domain. Both statements are **false against live state** and are corrected in that file as part of this issue.

Both `cdn` and `cdn2` are proxied CNAMEs to `queenzone.blob.core.windows.net`. Direct `https://queenzone-dev.azurewebsites.net/health` returns **403 Ip Forbidden** from non-Cloudflare clients — App Service origin lock is effective for the main site.

## Ownership matrix

Treatments:

- **import** — OpenTofu will import and manage.
- **data** — reference via data source / read-only; do not recreate.
- **outside** — keep outside OpenTofu permanently (or until a later explicit decision).
- **defer** — needs a decision or missing credentials before classifying.

### Azure (`Queenzone-RG`)

| Resource | ID (sanitised path) | Treatment | Notes / outage risk |
| --- | --- | --- | --- |
| Resource group `Queenzone-RG` | `/subscriptions/…/resourceGroups/Queenzone-RG` | import | australiaeast; container for the stack |
| Plan `ASP-Queenzone` | `…/Microsoft.Web/serverFarms/ASP-Queenzone` | import | **Never recreate** while site is live; SKU must stay B1×1 |
| Site `queenzone-dev` | `…/Microsoft.Web/sites/queenzone-dev` | import | System-assigned MI `2924c429-8228-430a-ae74-c514a18a7d0e`; Linux `DOTNETCORE\|10.0` |
| Hostname bindings `queenzone.org`, `www.queenzone.org` | `…/sites/queenzone-dev/hostNameBindings/…` | import | SNI certs bound; breaking bindings = public TLS outage |
| Certificates `queenzone.org`, `www.queenzone.org` | `…/Microsoft.Web/certificates/…` | import or defer | GeoTrust TLS RSA CA G1, expire **2026-12-29**; confirm renew path before encoding as managed vs uploaded |
| Access restrictions (Cloudflare IPv4/IPv6 allow + deny all) | site `ipSecurityRestrictions` | import | Mis-order or drop = either open origin or lock out Cloudflare |
| SCM access restrictions | site `scmIpSecurityRestrictions` | import | Currently **Allow all**; keep separate from main site rules (deploy path) |
| App settings (names only) | site config | defer → #618 | Names inventoried; **values** stay in Azure/Bitwarden, never state |
| SQL server `queenzone-sql-server` | `…/Microsoft.Sql/servers/queenzone-sql-server` | import | Public network enabled; AAD admin present; SQL auth still used by app |
| Firewall `AllowAllWindowsAzureIps` | `…/firewallRules/AllowAllWindowsAzureIps` | import | Required for App Service → SQL |
| Firewall `ClientIPAddress_2026-6-11_20-28-58` | `…/firewallRules/ClientIPAddress_…` | defer | Operator workstation IP; likely keep outside or replace with named break-glass rule |
| Database `queenzone-db` | `…/databases/queenzone-db` | import | Basic; **never recreate** (data loss). Schema via EF only |
| SQL auditing (server + db) | `…/auditingSettings/Default` | data / defer | Currently **Disabled** — do not “enable by default” in first import |
| Short-term backup (7 days, LRS) | backup policy | import | Provider default-ish for Basic; LTR all zero |
| Storage account `queenzone` | `…/storageAccounts/queenzone` | import | Shared key allowed; public blob access allowed; **custom domain `cdn.queenzone.org`** |
| Blob soft-delete / container soft-delete (7 days) | blob service properties | import | Versioning **not** enabled; no lifecycle management policy |
| Blob containers + public access flags | per-container | import | See [Storage containers](#storage-containers-live); changing ACLs can break media or expose private UGC |
| Storage RBAC assignments | scope storage account | data | Empty list at audit time (access via keys / portal roles at higher scope) |
| Log Analytics `queenzone-dev-law` | `…/workspaces/queenzone-dev-law` | import | Retention 30d; daily cap **0.1 GB** |
| App Insights `queenzone-dev-ai` | `…/components/queenzone-dev-ai` | import | Workspace-linked; retention 90d; daily volume cap 100 GB (platform billing cap) |
| Action group `queenzone-alerts` | `…/actionGroups/queenzone-alerts` | import | Email receiver present (address not recorded here) |
| Webtest `queenzone-dev-health` | `…/webtests/queenzone-dev-health` | import / defer | Targets `https://queenzone-dev.azurewebsites.net/health` — **blocked by Cloudflare IP allowlist** for external probes; fix URL or allowlist before trusting alert |
| Metric / query alerts | listed in import JSON | import | `queenzone-dev-failed-requests` is **disabled** on purpose |
| Smart detector `Failure Anomalies - queenzone-dev-ai` | alertsmanagement | defer | Dashboard-created default; prefer leave as provider default unless drift forces import |
| Diagnostic settings (web/sql/storage) | n/a | outside | None configured — do not invent |

### Cloudflare (API inventory complete)

Account id `f93121b2086286e79a7a9fdb8d03cb4c`. Zone id `079fc2f37095c82fb3a2b4da65718b2b` (`queenzone.org`, Free, full setup). NS: `daisy.ns.cloudflare.com`, `skip.ns.cloudflare.com`. DNSSEC disabled. Detail JSON: [`infra/import/cloudflare-hostnames.json`](../../infra/import/cloudflare-hostnames.json). Worker source snapshot: [`infra/import/workers/pictures-queenzone-org.js`](../../infra/import/workers/pictures-queenzone-org.js).

| Item | Treatment | Notes |
| --- | --- | --- |
| Zone `queenzone.org` | import (#626) | Free plan; never recreate casually |
| DNS `queenzone.org` A → `52.237.246.162` (proxied) | import | App Service inbound IP |
| DNS `www` CNAME → `queenzone-dev.azurewebsites.net` (proxied) | import | |
| DNS `cdn` / `cdn2` CNAME → `queenzone.blob.core.windows.net` (proxied) | import | Only **cdn2** has a Worker route |
| DNS `asverify.cdn` CNAME (DNS-only) | import | Azure Storage custom-domain verification |
| DNS `asuid` / Bing / Google TXT|CNAME verify records | import | Keep; not secrets |
| SSL/TLS mode **strict** | import | Confirmed Full (strict). Edge Universal SSL active (+ backup pack) |
| `min_tls_version` = `1.0` | defer | Dashboard default; decide before encoding as desired |
| Always HTTPS / TLS 1.3 / automatic HTTPS rewrites | import | on |
| Worker script `pictures-queenzone-org` | import | **Never recreate blindly**; route is cdn2 despite name |
| Worker route `cdn2.queenzone.org/*` → that script | import | Do **not** attach to `cdn` |
| Page Rules | outside | None |
| Custom WAF / Transform / Cache / Origin rulesets | outside | No zone entrypoint custom rulesets |
| Managed Free WAF / Normalization / DDoS L7 rulesets | outside | Provider defaults — do not copy rule bodies |
| Account billing / members | outside | |
| Domain registrar | outside | |
| `CLOUDFLARE_API_TOKEN_READONLY` (Bitwarden) | outside | Inventory/ops only; never OpenTofu state |

### GitHub Actions / Bitwarden

| Item | Treatment | Notes |
| --- | --- | --- |
| Workflows under `.github/workflows/` | outside | App deploy path stays GitHub; OpenTofu CI is a later issue (#625) |
| Environment `dev` | data / defer | No protection rules today; used by `deploy.yml` |
| Repo secret `BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN` | outside | Token only; never OpenTofu state |
| Repo variable `BITWARDEN_APP_SERVICE_DEPLOY_SECRETS` | outside | UUID→name mapping only |
| Legacy raw GH secrets (`AZURE_WEBAPP_PUBLISH_PROFILE`, migration connection strings, etc.) | defer | Names still present; Bitwarden is intended SoT — reconcile/delete later (#618) |
| Bitwarden project `Queenzone Development` | outside | Secret values; OpenTofu may later wire Key Vault / references but not store values |
| App Service publish profile | outside | Rotatable credential |

### Entra / identity (reference)

| Item | Treatment | Notes |
| --- | --- | --- |
| App Service system-assigned MI | import with site | Principal id recorded in import JSON |
| Entra app registrations (Admin / member OAuth) | outside / defer | Documented in `entra-admin-auth.md`; not Azure RG resources |
| SQL AAD admin | data | Login name known; do not put credentials in state |

## Storage containers (live)

| Container | publicAccess | Product role | OpenTofu note |
| --- | --- | --- | --- |
| Photo/archive galleries (`queen`, `freddie-mercury`, …) | `blob` | Public photos via `cdn` | Keep public blob read |
| `images`, `css`, `mp3`, `forum`, `avatars`, `album-or-single-covers`, … | `blob` or `container` (`css`) | Legacy public assets | Keep; `css` is listable |
| `songfiles` | **`blob` (public)** | Fan audio intended via `cdn2` + member UX | **Discrepancy:** docs say private; live is public. Direct blob URL works. Relates to [#177](https://github.com/richardorchard/QueenZone.Modern/issues/177) |
| `attachments` | **`blob` (public)** | Legacy forum files; app redirects after auth | URL guessing bypasses app gate. Relates to #177 / media lockdown |
| `databasebackup` | private | Backups | Keep private; **never** public |
| `ugc-avatars`, `ugc-forum` | private | Modern UGC | Keep private; app proxy. Relates to [#583](https://github.com/richardorchard/QueenZone.Modern/issues/583), [#584](https://github.com/richardorchard/QueenZone.Modern/issues/584) |
| `ugc-photos`, `ugc-articles` | **missing** | Mentioned in `blob-storage-ugc.md` | Create only when product needs them — do not invent in first import |
| `test` | `blob` | Scratch | Consider delete or exclude from prod module |

No storage lifecycle policy exists. Soft delete is 7 days for blobs and containers; versioning is off.

## App Service application setting names (values not recorded)

Present on `queenzone-dev` at audit time:

`APPLICATIONINSIGHTS_CONNECTION_STRING`, `Authentication__Discord__ClientId/Secret`, `Authentication__Facebook__ClientId/Secret`, `Authentication__GitHub__ClientId/Secret`, `Authentication__Google__ClientId/Secret`, `Authentication__Microsoft__ClientId/Secret`, `BlobUpload__PublicBaseUrl`, `ConnectionStrings__BlobStorage`, `ConnectionStrings__QueenZoneLegacy`, `DIAGNOSTICS_AZUREBLOBRETENTIONINDAYS`, `OPENROUTER_API_KEY`, `WEBSITE_HEALTHCHECK_MAXPINGFAILURES`, `WEBSITE_HTTPLOGGING_RETENTION_DAYS`, `Analytics__GoogleAnalyticsServiceAccountJson`, `Analytics__TrafficCacheMinutes`, `Analytics__GoogleAnalyticsPropertyId`, `AzureAd__Instance`, `AzureAd__TenantId`, `AzureAd__ClientId`, `AzureAd__ClientSecret`, `AzureAd__CallbackPath`, `Admin__AllowedEmails__0`, `Admin__AllowedEmails__1`, `SCM_DO_BUILD_DURING_DEPLOYMENT`, `ENABLE_ORYX_BUILD`.

Notable absences vs docs: `WEBSITE_WARMUP_PATH` / `WEBSITE_WARMUP_STATUSES` were **not** in the setting name list (health check path `/health` is configured on the site). Ownership of settings is [#618](https://github.com/richardorchard/QueenZone.Modern/issues/618).

## Resources that must never be recreated

Blind destroy/recreate of any of these is an outage or data-loss event:

1. Azure SQL database `queenzone-db` (and server if databases cannot move).
2. Storage account `queenzone` and its blob data.
3. App Service plan `ASP-Queenzone` / site `queenzone-dev` while DNS points at them (import in place).
4. Custom hostname bindings + bound certificates for `queenzone.org` / `www.queenzone.org`.
5. Cloudflare proxied DNS for apex/www/cdn/cdn2 and the **cdn2 Worker**.
6. Azure Storage custom domain association for `cdn.queenzone.org`.
7. App Service access restrictions that deny non-Cloudflare ingress (unless carefully replaced).

## Suggested import order (later issues)

Documented for [#622](https://github.com/richardorchard/QueenZone.Modern/issues/622) / [#628](https://github.com/richardorchard/QueenZone.Modern/issues/628) / [#626](https://github.com/richardorchard/QueenZone.Modern/issues/626) — do not execute until remote state (#616) and safety controls (#619) exist.

1. Resource group (or data-source it).
2. Log Analytics workspace → Application Insights.
3. App Service plan → web app (no hostname swap).
4. Certificates → hostname bindings.
5. Access restriction rules (validate Cloudflare still serves `/health` after plan).
6. Action group → alerts / webtest (fix webtest URL when importing).
7. SQL server → firewall rules → database (import existing; never `create`).
8. Storage account → blob service properties → containers → custom domain.
9. Cloudflare zone/DNS → TLS → Worker/routes for **cdn2 only**.
10. App settings strategy (#618) last — names/references only.

**Outage risks during import:** hostname/TLS drift, IP restriction mistakes, storage public-access flips, Worker route removal on cdn2, SQL firewall removing `AllowAllWindowsAzureIps`.

## Dashboard defaults — do not copy blindly

| Observation | Guidance |
| --- | --- |
| Smart detection failure anomalies rule | Leave as Azure default unless product wants it managed |
| App Insights billing cap 100 GB | Far above LAW 0.1 GB daily cap; LAW cap is the real budget control — do not “harmonise” upward |
| `use32BitWorkerProcess: true` on a Linux .NET site | Likely portal noise; verify before encoding |
| Disabled metric alert `queenzone-dev-failed-requests` | Keep disabled; replaced by query alert |
| SQL auditing disabled | Import as disabled; enabling is a product decision |
| Storage versioning off | Do not enable in first apply |
| Operator SQL firewall ClientIP rule | Do not encode personal IPs as production IaC without renaming |
| Legacy GH secrets alongside Bitwarden | Clean up separately; do not duplicate into OpenTofu |
| Cloudflare `min_tls_version` = `1.0` | Free-plan dashboard default; decide before treating as desired state |
| Cloudflare managed Free WAF / Normalization / DDoS rulesets | Leave as provider defaults; do not import rule bodies |
| Worker script name `pictures-queenzone-org` | Historical; live route is cdn2 — do not “fix” by attaching it to cdn |

## Dependencies on open product issues

| Issue | Relevance |
| --- | --- |
| [#177](https://github.com/richardorchard/QueenZone.Modern/issues/177) | `songfiles` (and likely `attachments`) are publicly readable today; lockdown changes container ACL + possibly cdn2/auth design before OpenTofu freezes “desired” publicAccess |
| [#583](https://github.com/richardorchard/QueenZone.Modern/issues/583) | Anonymous `/ugc` proxy sensitivity — private containers must stay private |
| [#584](https://github.com/richardorchard/QueenZone.Modern/issues/584) | Upload API container narrowing — affects which containers exist and who may write |
| [#428](https://github.com/richardorchard/QueenZone.Modern/issues/428) | Cloudflare proxy / origin restriction history — current live state already restricts App Service to Cloudflare IPs |
| [#618](https://github.com/richardorchard/QueenZone.Modern/issues/618) | Secret-safe App Service configuration ownership |

## Follow-ups (non-blocking for #624)

1. Optional: **Storage Blob Data Reader** on `queenzone` for private-container object audits without account keys.
2. Confirm Azure App Service certificate renewal path (GeoTrust uploads expire **2026-12-29**) before #622 imports certs — not Cloudflare Origin CA.
3. Product decision whether Worker should set `Content-Disposition` for audio downloads (capability exists; live script does not).

No infrastructure mutation was performed for this audit.
