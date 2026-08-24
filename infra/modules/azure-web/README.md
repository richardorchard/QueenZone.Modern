# Azure web module

This module owns the imported production App Service plan, Linux web app,
workspace-linked telemetry, custom hostname bindings, TLS configuration, and
the existing Cloudflare-only main-site ingress policy.

The SCM endpoint deliberately keeps its separate allow-all default policy so
the existing application deployment workflow remains available. The main-site
deny is already live. Cloudflare's published IP list is the source of truth
for the allow rules; do not regroup the packed CIDR strings, and add a new
range only in a reviewed plan after `Test-CloudflareOriginCidrs.ps1` fails.

Uploaded App Service certificate resources remain outside OpenTofu. AzureRM
cannot describe them without the private PFX material, and their renewal path
has not been confirmed. The hostname resources retain the current SNI state and
certificate thumbprints without putting certificate secrets in state.

Every irreplaceable resource must include `lifecycle { prevent_destroy = true }`.
Do not use broad `ignore_changes`; record each externally owned attribute and
its reason. OpenTofu never manages `app_settings` or `connection_string` under
[ADR 0008](../../../docs/decisions/0008-app-service-settings-ownership.md).
The site therefore ignores `app_settings` and omits the unused
`connection_string` collection.
