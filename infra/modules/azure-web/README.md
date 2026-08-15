# Azure web module

This module owns the imported production App Service plan, Linux web app,
workspace-linked telemetry, custom hostname bindings, TLS configuration, and
the existing Cloudflare-only main-site ingress policy.

The SCM endpoint deliberately keeps its separate allow-all default policy so
the existing application deployment workflow remains available. The main-site
deny is already live; changes to its Cloudflare ranges must be coordinated with
the Cloudflare edge stack.

Uploaded App Service certificate resources remain outside OpenTofu. AzureRM
cannot describe them without the private PFX material, and their renewal path
has not been confirmed. The hostname resources retain the current SNI state and
certificate thumbprints without putting certificate secrets in state.

App settings are explicitly ignored pending #618. This is a narrow exception:
the map contains secrets and is split between ARM deployment automation and
operators. OpenTofu must not read or replace those values.
