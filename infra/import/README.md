# Sanitised OpenTofu import inventory

Read-only audit artefacts for [#624](https://github.com/richardorchard/QueenZone.Modern/issues/624).

| File | Contents |
| --- | --- |
| `azure-resources.json` | Azure resource IDs, SKUs, treatments, never-recreate flags |
| `storage-containers.json` | Container public-access flags and related issues |
| `cloudflare-hostnames.json` | Zone/account IDs, DNS, SSL, Worker route, rules treatment |
| `workers/pictures-queenzone-org.js` | Live Worker source snapshot (no secrets) |
| `github-bitwarden.json` | Environment/secret/setting *names* only |

Narrative ownership matrix: [`docs/architecture/opentofu-inventory.md`](../../docs/architecture/opentofu-inventory.md).

No secret values, connection strings, publish profiles, or tokens are stored here.
