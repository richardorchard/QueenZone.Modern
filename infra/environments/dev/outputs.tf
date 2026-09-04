output "web_app_id" {
  value = module.azure_web.web_app_id
}

output "default_hostname" {
  value = module.azure_web.default_hostname
}

output "custom_domain_verification_id" {
  sensitive = true
  value     = module.azure_web.custom_domain_verification_id
}

output "managed_identity_principal_id" {
  value = module.azure_web.managed_identity_principal_id
}

output "dev_sql_database_id" {
  description = "ID of the empty dev-only database on the existing logical server."
  value       = module.azure_data.sql_database_id
}

output "dev_storage_account_id" {
  description = "ID of the dev-only media storage account without exporting keys."
  value       = module.azure_data.storage_account_id
}
