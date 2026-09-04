locals {
  ownership_boundary = {
    azure_resource_group = var.azure_resource_group_name
    cloudflare_zone      = var.cloudflare_zone_name
    environment          = var.environment
  }
}

check "production_scale_contract" {
  assert {
    condition     = var.app_service_sku == "B1" && var.app_service_worker_count == 1
    error_message = "Production must remain B1 with one worker unless the accepted hosting decision changes."
  }
}

resource "azurerm_resource_group" "production" {
  name     = var.azure_resource_group_name
  location = var.azure_location

  lifecycle {
    prevent_destroy = true
  }
}

module "azure_web" {
  source = "../../modules/azure-web"

  resource_group_name = azurerm_resource_group.production.name
  location            = azurerm_resource_group.production.location
  sku_name            = var.app_service_sku
  worker_count        = var.app_service_worker_count
}

module "azure_data" {
  source = "../../modules/azure-data"

  resource_group_id   = azurerm_resource_group.production.id
  resource_group_name = var.azure_resource_group_name
  location            = var.azure_location
}

# azure-data can also attach a database to an existing logical server for the
# dev root. Preserve these imported production resource addresses as indexed
# optional resources without proposing a replacement.
moved {
  from = module.azure_data.azapi_resource.sql_server
  to   = module.azure_data.azapi_resource.sql_server[0]
}

moved {
  from = module.azure_data.azurerm_mssql_firewall_rule.azure_services
  to   = module.azure_data.azurerm_mssql_firewall_rule.azure_services[0]
}

moved {
  from = module.azure_data.azurerm_mssql_server_extended_auditing_policy.production
  to   = module.azure_data.azurerm_mssql_server_extended_auditing_policy.production[0]
}

moved {
  from = module.azure_data.azapi_resource.blob_service
  to   = module.azure_data.azapi_resource.blob_service[0]
}

module "azure_mobile_builds" {
  source = "../../modules/azure-mobile-builds"

  resource_group_id = azurerm_resource_group.production.id
  location          = azurerm_resource_group.production.location
}

module "cloudflare_edge" {
  source = "../../modules/cloudflare-edge"

  account_id = var.cloudflare_account_id
  zone_id    = var.cloudflare_zone_id
  zone_name  = var.cloudflare_zone_name
}

# All modules use declarative imports in imports.tf; an apply must never
# precede plan review.
