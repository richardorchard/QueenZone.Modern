# Cloudflare edge module

This module owns the imported `queenzone.org` zone, inventory-confirmed DNS
records, Full (strict) TLS and related zone settings, the `pictures-queenzone-org`
Worker on `cdn2.queenzone.org/*`, and the `pictures-legacy-redirect` Worker on
`pictures.queenzone.org/*`.

`cdn.queenzone.org` remains a straight Cloudflare proxy to Azure Blob Storage.
Do not attach a Worker to it. Worker source in `workers/` must stay LF and is
the intended deployment copy. The first import ignores `content` so it cannot
republish a live Worker; remove that ignore only after a refresh-only plan is
no-op.

The provider reads `CLOUDFLARE_API_TOKEN` from the environment. Never pass a
token as a variable, and never write token values into HCL, plan files, or
state. Use the narrowly scoped plan/apply tokens documented in
[`opentofu-state-and-identity.md`](../../../docs/architecture/opentofu-state-and-identity.md).

QueenZone owns no Page Rules and no custom WAF, Transform, Cache, or Origin
rulesets. Managed Free WAF / Normalization / DDoS rulesets stay outside this
module. `min_tls_version` remains the dashboard default (`1.0`) and is not
encoded.

The zone, public DNS records, Worker scripts, and Worker routes must include
`lifecycle { prevent_destroy = true }`.
