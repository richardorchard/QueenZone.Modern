# Azure web module

Issue #622 will declare and import the existing App Service plan, Linux web app, telemetry, hostname bindings, certificates, and ingress controls here.

Every irreplaceable resource must include `lifecycle { prevent_destroy = true }`. Do not use broad `ignore_changes`; record each externally owned attribute and its reason. `app_settings` and `connection_string` must be omitted or covered by `lifecycle { ignore_changes = [app_settings, connection_string] }` on the site resource — see [ADR 0008](../../../docs/decisions/0008-app-service-settings-ownership.md) (#618), which decided OpenTofu never manages that map.
