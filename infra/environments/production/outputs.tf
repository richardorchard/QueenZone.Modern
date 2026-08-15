output "ownership_boundary" {
  description = "Non-sensitive production ownership boundary used during review and imports."
  value       = local.ownership_boundary
}

output "azure_subscription_id" {
  description = "Azure subscription containing the existing production estate."
  value       = var.azure_subscription_id
}

output "cloudflare_scope" {
  description = "Non-sensitive Cloudflare account and zone IDs."
  value = {
    account_id = var.cloudflare_account_id
    zone_id    = var.cloudflare_zone_id
    zone_name  = var.cloudflare_zone_name
  }
}

output "module_import_contracts" {
  description = "Non-sensitive live names and IDs that later resource imports must match."
  value = {
    azure_web       = module.azure_web.import_contract
    azure_data      = module.azure_data.import_contract
    cloudflare_edge = module.cloudflare_edge.import_contract
  }
}

output "azure_web_identity_principal_id" {
  description = "System-assigned identity principal ID; no direct role assignments were present at the 2026-08-15 audit."
  value       = module.azure_web.managed_identity_principal_id
}
