# Cloudflare edge module

Issue [#626](https://github.com/richardorchard/QueenZone.Modern/issues/626) declares and imports the existing `queenzone.org` zone, DNS records, TLS/security/cache zone settings, both Workers, and their routes. The provider reads `CLOUDFLARE_API_TOKEN` from the environment; never pass a token as a variable.

The zone, public DNS records, both Worker scripts, and both Worker routes set `lifecycle { prevent_destroy = true }`. `min_tls_version` is intentionally left unmanaged: it is a dashboard default (`1.0`), not a reviewed decision (see `docs/architecture/opentofu-inventory.md`).

## Hostnames

- `cdn.queenzone.org` is a straight proxy to Azure Blob Storage. It has **no** Worker route.
- `cdn2.queenzone.org` carries the `pictures-queenzone-org` Worker on route `cdn2.queenzone.org/*`. Do not attach this Worker to `cdn`.
- `pictures.queenzone.org` is a retired hostname kept for compatibility. It carries the `pictures-legacy-redirect` Worker on route `pictures.queenzone.org/*`, which serves `robots.txt` and 301s everything else to `cdn.queenzone.org`.

## Unrecorded values

Three TXT records (`asuid.queenzone.org`, `asuid.www.queenzone.org`, the apex Google site-verification record) exist live but their content values were not captured during the #624 audit. Their resources set `ignore_changes = [content]` so import reads the live value and this module never proposes overwriting it.

Worker runtime knobs not captured during the audit (compatibility date/flags, usage model, bindings, observability, migrations, assets) are similarly ignored so this module cannot silently change published Worker behaviour.
