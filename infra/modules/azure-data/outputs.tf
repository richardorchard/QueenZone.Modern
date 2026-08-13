output "import_contract" {
  description = "Non-sensitive names that later Azure data imports must match."
  value = {
    resource_group  = var.resource_group_name
    sql_server      = var.sql_server_name
    sql_database    = var.sql_database_name
    storage_account = var.storage_account_name
  }
}
