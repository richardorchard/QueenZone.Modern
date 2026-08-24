# OpenTofu Cloudflare edge import

Issue: [#626](https://github.com/richardorchard/QueenZone.Modern/issues/626),
step 6 of epic [#615](https://github.com/richardorchard/QueenZone.Modern/issues/615).

## Managed boundary

The production root declares imports for the live `queenzone.org` zone and the
inventory-confirmed edge resources:

- proxied apex `A` to the App Service inbound IP;
- proxied `www` CNAME to `queenzone-dev.azurewebsites.net`;
- proxied `cdn` and `cdn2` CNAMEs to `queenzone.blob.core.windows.net`;
- DNS-only `dev` CNAME for mobile test-build downloads;
- retired `pictures` CNAME plus its compatibility Worker;
- Azure Storage / App Service / Google / Bing verification records;
- Full (strict) TLS, Always HTTPS, TLS 1.3, automatic HTTPS rewrites, and the
  other zone settings the 2026-08-12 inventory marked `import`;
- Worker `pictures-queenzone-org` on `cdn2.queenzone.org/*`;
- Worker `pictures-legacy-redirect` on `pictures.queenzone.org/*`.

`cdn.queenzone.org` stays a straight Cloudflare proxy. There is no Worker route
on `cdn`. QueenZone owns no Page Rules and no custom WAF, Transform, Cache, or
Origin rulesets; managed Free WAF / Normalization / DDoS rulesets stay outside
OpenTofu. `min_tls_version` remains the dashboard default (`1.0`) and is not
encoded.

Worker source lives in
[`infra/modules/cloudflare-edge/workers/`](../../infra/modules/cloudflare-edge/workers/)
and must stay LF (see `.gitattributes`). Those files are the intended
deployment copy. The snapshots under
[`infra/import/workers/`](../../infra/import/workers/) remain the 2026-08-16
audit artefacts.

The first apply is import-only. Cloudflare provider 5.23 reports a Worker
`content` rewrite plus computed metadata (`etag`, `last_deployed_from`,
handlers) even when the script body matches live. The Worker resources
therefore ignore `content` and those computed fields so an import cannot
republish `cdn2`. After the import is in state, run a refresh-only plan
before removing `content` from `ignore_changes`. Do not treat an unexplained
Worker content update as a routine publish.

The beta `cloudflare_worker` / `cloudflare_worker_version` /
`cloudflare_workers_deployment` resources are not used. Importing them needs
live version IDs the inventory did not pin, and declaring them from source
would publish a new version. `cloudflare_workers_script` plus
`cloudflare_workers_route` matches the live scripts and routes.

## Origin trust boundary

Cloudflare's published IP list (`data.cloudflare_ip_ranges` and
`https://api.cloudflare.com/client/v4/ips`) is the source of truth for App
Service allow rules. The live main-site restriction remains the three packed
Cloudflare allow rules imported by #622, plus the existing deny-all default.

AzureRM 5.0.1 still normalises the explicit deny-all and SCM allow-all rules to
empty default-action fields. Those two provider fields stay in
`ignore_changes` so an apply cannot silently open the origin or lock out SCM.
The allow CIDRs themselves remain managed. A production check and
[`scripts/Test-CloudflareOriginCidrs.ps1`](../../scripts/Test-CloudflareOriginCidrs.ps1)
fail if Cloudflare publishes a range that Azure does not allow.

Re-verified on **2026-08-24**: the published IPv4 and IPv6 lists still match
the imported packed strings. Adding a new Cloudflare range is a reviewed
in-place App Service update, not a first-import change.

## Credentials

The Cloudflare provider reads `CLOUDFLARE_API_TOKEN` from the process
environment. Never pass the token as a variable. Create the narrowly scoped
`CLOUDFLARE_API_TOKEN_TOFU_PLAN` and `CLOUDFLARE_API_TOKEN_TOFU_APPLY` secrets
documented in [`opentofu-state-and-identity.md`](opentofu-state-and-identity.md)
before the first remote Cloudflare plan. Do not reuse
`CLOUDFLARE_WORKER_READWRITE` as the apply token unless its zone/DNS/settings
scopes are reviewed.

Local `scripts/Test-OpenTofu.ps1` (backend=false) does not need a Cloudflare
token. A production plan that refreshes Cloudflare resources does.

## Verification contract

Keep plan files outside the repository and report resource actions only. A
valid first Cloudflare plan must show imports with no DNS replacement, Worker
route move, TLS-mode change, or unexplained create/update/delete.

A remote plan on **2026-08-24** against production state showed every
Cloudflare address as import-only: zone, 12 DNS records, 15 zone settings, both
Workers, and both routes. No Cloudflare create, update, replace, or destroy.
The remaining in-place updates were the existing AzAPI empty-`output`
state-only diffs from #628. The plan was not applied. Dedicated
`CLOUDFLARE_API_TOKEN_TOFU_PLAN` / `_APPLY` secrets are still missing; this
plan used the inventory read-only token.

Read-only live checks on **2026-08-24**, before any Cloudflare apply:

- `scripts/Test-OpenTofu.ps1` passed (format, lifecycle, origin CIDR coverage, `validate`);
- `scripts/Test-CloudflareOriginCidrs.ps1` passed: 15 published IPv4 and 7 published IPv6 prefixes are already allowed;
- `www` and apex `/health` returned 200 with `CF-Ray`;
- `cdn` photo responses returned 200, `Cache-Control: max-age=14400`, no Worker CORS/`nosniff` headers, and no `Content-Disposition`;
- `cdn2` photo responses returned 200 with Worker `Cache-Control`, `Access-Control-Allow-Origin: *`, and `X-Content-Type-Options: nosniff`, and no `Content-Disposition`;
- `cdn2` `/songfiles/*` returned 404;
- `pictures.queenzone.org` redirected permanently to the matching `cdn` URL; `/robots.txt` returned 200;
- direct GET `https://queenzone-dev.azurewebsites.net/health` returned 403;
- the SCM site remained reachable;
- `scripts/Smoke-LiveSite.ps1` passed.

Live checks after import, before any intentional change:

1. Confirm every published Cloudflare CIDR is already in the App Service allow
   list (`Test-CloudflareOriginCidrs.ps1`).
2. Probe `www`, apex, `cdn`, and `cdn2` for `CF-Ray` and the expected cache /
   content-disposition behaviour.
3. Confirm the main-site deny-all is still live: direct GET
   `https://queenzone-dev.azurewebsites.net/health` returns 403.
4. Confirm SCM remains separately reachable.
5. Run the full [`scripts/Smoke-LiveSite.ps1`](../../scripts/Smoke-LiveSite.ps1)
   suite.

## Rollback order (origin lockout)

Do not apply a plan that removes Cloudflare allow rules before opening another
ingress path. If an apply goes wrong, recover in this order:

1. **Do not delete or replace DNS.** Edit the existing apex/`www`/`cdn`/`cdn2`
   records in place, or `tofu state rm` the drifted record and stop. A
   replacement can drop the proxy flag or the Storage custom-domain proof.
2. **Restore Worker routes before Worker source.** If `cdn2.queenzone.org/*`
   lost `pictures-queenzone-org`, re-attach that route first. `cdn` is
   independent and should still serve photos. If `pictures.queenzone.org/*`
   lost its route, re-attach `pictures-legacy-redirect`; it is compatibility
   only.
3. **Keep or restore every Cloudflare CIDR on the App Service** before
   touching the deny-all default. Adding a missing Cloudflare range is safe;
   removing an allow rule or changing the default action is how origin
   lockout happens.
4. **Leave SCM allow-all alone.** It is the break-glass deploy path when the
   main site is locked to Cloudflare. Do not set
   `scm_use_main_ip_restriction = true`.
5. **Only after public `www` / apex `/health` succeed**, confirm that direct
   Azure `/health` is still 403.
6. If OpenTofu state and Cloudflare disagree, restore the previous state
   blob version from [`opentofu-state-and-identity.md`](opentofu-state-and-identity.md)
   rather than recreating the zone, DNS, or Workers.

Never run `tofu destroy` against this stack. `prevent_destroy` blocks destroy
and replacement of the zone, DNS, Workers, and routes; it does not block
in-place TTL, proxy-flag, TLS-mode, or Worker-content updates.
