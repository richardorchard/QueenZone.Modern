# Sanitised OpenTofu import inventory

Read-only audit artefacts for [#624](https://github.com/richardorchard/QueenZone.Modern/issues/624).

| File | Contents |
| --- | --- |
| `azure-resources.json` | Azure resource IDs, SKUs, treatments, never-recreate flags |
| `storage-containers.json` | Container public-access flags and related issues |
| `cloudflare-hostnames.json` | Zone/account IDs, DNS, SSL, Worker route, rules treatment |
| `workers/pictures-queenzone-org.js` | 2026-08-16 audit snapshot for hostname `cdn2.queenzone.org`. Deployment copy: `infra/modules/cloudflare-edge/workers/`. |
| `workers/pictures-legacy-redirect.js` | 2026-08-16 audit snapshot for `pictures.queenzone.org`. Deployment copy: `infra/modules/cloudflare-edge/workers/`. |
| `ownership-matrix.csv` | Compact treatment matrix (import / data / outside / defer) |
| `github-bitwarden.json` | Environment/secret/setting *names* only; `appServiceSettingNames` is also the required-name list checked nightly by `scripts/Test-AppServiceSettingNames.ps1` (ADR 0008) |

Narrative ownership matrix: [`docs/architecture/opentofu-inventory.md`](../../docs/architecture/opentofu-inventory.md).

No secret values, connection strings, publish profiles, or tokens are stored here.
