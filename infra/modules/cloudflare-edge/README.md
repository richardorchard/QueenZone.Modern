# Cloudflare edge module

Issue #626 will declare and import the existing zone, DNS, TLS settings, Worker, and route here. The provider reads `CLOUDFLARE_API_TOKEN` from the environment; never pass a token as a variable.

The zone, public DNS records, Worker, and Worker route must include `lifecycle { prevent_destroy = true }`. The Worker route is `cdn2.queenzone.org/*`; `cdn.queenzone.org` remains a straight proxy to Azure Blob Storage.
