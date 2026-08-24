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

check "cloudflare_origin_cidrs_allowed" {
  assert {
    condition = (
      length(setsubtract(toset(module.cloudflare_edge.published_ipv4_cidrs), toset(module.azure_web.origin_allow_ipv4_cidrs))) == 0 &&
      length(setsubtract(toset(module.cloudflare_edge.published_ipv6_cidrs), toset(module.azure_web.origin_allow_ipv6_cidrs))) == 0
    )
    error_message = "Every published Cloudflare IPv4 and IPv6 origin CIDR must stay allowed on the App Service."
  }
}

check "cdn_worker_boundary" {
  assert {
    condition     = !contains(module.cloudflare_edge.worker_route_patterns, "cdn.queenzone.org/*")
    error_message = "cdn.queenzone.org must not have a Worker route."
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

# Azure and Cloudflare resources use declarative imports in imports.tf.
# An apply must never precede plan review. The first Cloudflare apply is
# import-only; do not publish Worker source or rewrite DNS from a local session.
