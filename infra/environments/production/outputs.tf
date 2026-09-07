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
    mobile_builds   = module.azure_mobile_builds.import_contract
    cloudflare_edge = module.cloudflare_edge.import_contract
  }
}

output "azure_web_identity_principal_id" {
  description = "System-assigned identity principal ID; no direct role assignments were present at the 2026-08-15 audit."
  value       = module.azure_web.managed_identity_principal_id
}

output "migration_target" {
  description = "Non-sensitive Phase 7 target names used by the staged migration runbook."
  value = {
    location        = var.production_target_location
    web_app         = module.azure_web_target.import_contract.web_app
    service_plan    = module.azure_web_target.import_contract.service_plan
    log_analytics   = module.azure_web_target.import_contract.log_analytics
    app_insights    = module.azure_web_target.import_contract.app_insights
    sql_server      = module.azure_data_target.import_contract.sql_server
    sql_database    = module.azure_data_target.import_contract.sql_database
    storage_account = module.azure_data_target.import_contract.storage_account
  }
}
