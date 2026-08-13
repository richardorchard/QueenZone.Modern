# Azure web module

Issue #622 will declare and import the existing App Service plan, Linux web app, telemetry, hostname bindings, certificates, and ingress controls here.

Every irreplaceable resource must include `lifecycle { prevent_destroy = true }`. Do not use broad `ignore_changes`; record each externally owned attribute and its reason. App settings remain outside this module until #618 decides ownership.
