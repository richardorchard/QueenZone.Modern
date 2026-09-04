output "import_contract" {
  description = "Non-sensitive names that later Azure data imports must match."
  value = {
    resource_group  = var.resource_group_name
    sql_server      = var.sql_server_name
    sql_database    = var.sql_database_name
    storage_account = var.storage_account_name
    containers      = sort(keys(var.containers))
  }
}

output "storage_account_id" {
  description = "Managed Storage account resource ID without exporting account keys."
  value       = azapi_resource.storage_account.id
}

output "sql_database_id" {
  description = "Managed SQL database resource ID."
  value       = azurerm_mssql_database.production.id
}
