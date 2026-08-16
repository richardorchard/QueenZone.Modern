locals {
  azure_resource_group_id = "/subscriptions/${var.azure_subscription_id}/resourceGroups/${var.azure_resource_group_name}"
  azure_web_base_id       = "${local.azure_resource_group_id}/providers/Microsoft.Web"
  azure_monitor_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.Insights"
  log_analytics_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.OperationalInsights"
  azure_sql_base_id       = "${local.azure_resource_group_id}/providers/Microsoft.Sql"
  azure_storage_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.Storage"
  azure_data_containers = toset([
    "album-or-single-covers", "attachments", "avatars", "brian-may",
    "css", "databasebackup", "fan-art", "fan-pics", "forum",
    "freddie-mercury", "freddie-tribute-concert", "images", "john-deacon",
    "miscellaneous", "mp3", "pre-queen", "queen",
    "queen-and-adam-lambert", "queen-and-paul-rodgers",
    "queen-memorabillia", "roger-taylor", "songfiles", "special-events",
    "ugc-avatars", "ugc-forum", "us-convention-2001",
  ])
}

import {
  to = azurerm_resource_group.production
  id = local.azure_resource_group_id
}

import {
  to = module.azure_web.azurerm_log_analytics_workspace.production
  id = "${local.log_analytics_base_id}/workspaces/queenzone-dev-law"
}

import {
  to = module.azure_web.azurerm_application_insights.production
  id = "${local.azure_monitor_base_id}/components/queenzone-dev-ai"
}

import {
  to = module.azure_web.azurerm_service_plan.production
  id = "${local.azure_web_base_id}/serverFarms/ASP-Queenzone"
}

import {
  to = module.azure_web.azurerm_linux_web_app.production
  id = "${local.azure_web_base_id}/sites/queenzone-dev"
}

import {
  to = module.azure_web.azurerm_app_service_custom_hostname_binding.production["queenzone.org"]
  id = "${local.azure_web_base_id}/sites/queenzone-dev/hostNameBindings/queenzone.org"
}

import {
  to = module.azure_web.azurerm_app_service_custom_hostname_binding.production["www.queenzone.org"]
  id = "${local.azure_web_base_id}/sites/queenzone-dev/hostNameBindings/www.queenzone.org"
}

import {
  to = module.azure_data.azapi_resource.sql_server
  id = "${local.azure_sql_base_id}/servers/queenzone-sql-server"
}

import {
  to = module.azure_data.azurerm_mssql_firewall_rule.azure_services
  id = "${local.azure_sql_base_id}/servers/queenzone-sql-server/firewallRules/AllowAllWindowsAzureIps"
}

import {
  to = module.azure_data.azurerm_mssql_database.production
  id = "${local.azure_sql_base_id}/servers/queenzone-sql-server/databases/queenzone-db"
}

import {
  to = module.azure_data.azurerm_mssql_server_extended_auditing_policy.production
  id = "${local.azure_sql_base_id}/servers/queenzone-sql-server/extendedAuditingSettings/Default"
}

import {
  to = module.azure_data.azurerm_mssql_database_extended_auditing_policy.production
  id = "${local.azure_sql_base_id}/servers/queenzone-sql-server/databases/queenzone-db/extendedAuditingSettings/Default"
}

import {
  to = module.azure_data.azapi_resource.storage_account
  id = "${local.azure_storage_base_id}/storageAccounts/queenzone"
}

import {
  to = module.azure_data.azapi_resource.blob_service
  id = "${local.azure_storage_base_id}/storageAccounts/queenzone/blobServices/default"
}

import {
  for_each = local.azure_data_containers
  to       = module.azure_data.azapi_resource.container[each.value]
  id       = "${local.azure_storage_base_id}/storageAccounts/queenzone/blobServices/default/containers/${each.value}"
}
