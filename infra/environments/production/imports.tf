locals {
  azure_resource_group_id = "/subscriptions/${var.azure_subscription_id}/resourceGroups/${var.azure_resource_group_name}"
  azure_web_base_id       = "${local.azure_resource_group_id}/providers/Microsoft.Web"
  azure_monitor_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.Insights"
  log_analytics_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.OperationalInsights"
  azure_sql_base_id       = "${local.azure_resource_group_id}/providers/Microsoft.Sql"
  azure_storage_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.Storage"
  mobile_build_storage_id = "${local.azure_storage_base_id}/storageAccounts/queenzonemobilebuilds"
  azure_data_containers = toset([
    "album-or-single-covers", "attachments", "avatars", "brian-may",
    "css", "databasebackup", "fan-art", "fan-pics", "forum",
    "freddie-mercury", "freddie-tribute-concert", "images", "john-deacon",
    "miscellaneous", "mp3", "pre-queen", "queen",
    "queen-and-adam-lambert", "queen-and-paul-rodgers",
    "queen-memorabillia", "roger-taylor", "songfiles", "special-events",
    "ugc-avatars", "ugc-forum", "us-convention-2001",
  ])
  cloudflare_zone_id    = var.cloudflare_zone_id
  cloudflare_account_id = var.cloudflare_account_id
  cloudflare_dns_record_ids = {
    apex          = "c22f8759158c7a3f06e290e5f51f5da8"
    www           = "fdd05b163df7c2941ae1e36986558228"
    dev           = "299da79a473f1c6bc03dc8a2f735269a"
    cdn           = "c8989e49d2d624756ddf35a05e3ac153"
    cdn2          = "1af26073508a0494bc5d66f5b23df57f"
    pictures      = "4bc9968da3b9ec19d792fef11ef4d77a"
    asverify_cdn  = "9b996207c2701bd567962763ed653142"
    asuid         = "afd377459a21a49b39458293e929221a"
    asuid_www     = "b3d6ee3412118593835755b3a424f0c8"
    google_site   = "4d6c859c709bc0f9382ba74f93465b89"
    bing_verify_1 = "30eaa5bf1d4ad3834d5c7db37174c83b"
    bing_verify_2 = "fd000f4e7e714d719eb3ae0618e9ff4c"
  }
  cloudflare_worker_route_ids = {
    cdn2            = "0fd4ebdc9c3e4825a1d9e6527fbd4d24"
    legacy_pictures = "276e102a9ef8402c9b610c7bc60bbedb"
  }
  cloudflare_string_zone_settings = toset([
    "ssl", "always_use_https", "tls_1_3", "automatic_https_rewrites",
    "security_level", "cache_level", "browser_check", "brotli",
    "http2", "http3", "websockets", "polish", "hotlink_protection",
    "development_mode",
  ])
}

import {
  to = module.azure_mobile_builds.azapi_resource.storage_account
  id = local.mobile_build_storage_id
}

import {
  to = module.azure_mobile_builds.azapi_resource.blob_service
  id = "${local.mobile_build_storage_id}/blobServices/default"
}

import {
  to = module.azure_mobile_builds.azapi_resource.builds_container
  id = "${local.mobile_build_storage_id}/blobServices/default/containers/builds"
}

import {
  to = module.azure_mobile_builds.azurerm_role_assignment.mobile_publisher
  id = "${local.mobile_build_storage_id}/providers/Microsoft.Authorization/roleAssignments/9b23875e-46d9-4393-9ee7-1fec8a74fd7c"
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

import {
  to = module.cloudflare_edge.cloudflare_zone.production
  id = local.cloudflare_zone_id
}

import {
  for_each = local.cloudflare_dns_record_ids
  to       = module.cloudflare_edge.cloudflare_dns_record.this[each.key]
  id       = "${local.cloudflare_zone_id}/${each.value}"
}

import {
  for_each = local.cloudflare_string_zone_settings
  to       = module.cloudflare_edge.cloudflare_zone_setting.this[each.value]
  id       = "${local.cloudflare_zone_id}/${each.value}"
}

import {
  to = module.cloudflare_edge.cloudflare_zone_setting.challenge_ttl
  id = "${local.cloudflare_zone_id}/challenge_ttl"
}

import {
  to = module.cloudflare_edge.cloudflare_workers_script.cdn2
  id = "${local.cloudflare_account_id}/pictures-queenzone-org"
}

import {
  to = module.cloudflare_edge.cloudflare_workers_script.legacy_pictures
  id = "${local.cloudflare_account_id}/pictures-legacy-redirect"
}

import {
  to = module.cloudflare_edge.cloudflare_workers_route.cdn2
  id = "${local.cloudflare_zone_id}/${local.cloudflare_worker_route_ids.cdn2}"
}

import {
  to = module.cloudflare_edge.cloudflare_workers_route.legacy_pictures
  id = "${local.cloudflare_zone_id}/${local.cloudflare_worker_route_ids.legacy_pictures}"
}
