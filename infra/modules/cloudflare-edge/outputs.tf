output "import_contract" {
  description = "Non-sensitive IDs and names that later Cloudflare imports must match."
  value = {
    account_id                  = var.account_id
    zone_id                     = var.zone_id
    zone_name                   = var.zone_name
    worker_name                 = var.worker_name
    worker_route                = var.worker_route
    legacy_redirect_worker_name = var.legacy_redirect_worker_name
    legacy_redirect_route       = var.legacy_redirect_route
  }
}
