# Fresh resources, separate state. No imports or production resources here.
resource "azurerm_resource_group" "dev" {
  name     = "Queenzone-Dev-RG"
  location = "australiaeast"
  lifecycle {
    prevent_destroy = true
  }
}

module "azure_web" {
  source                       = "../../modules/azure-web"
  environment_name             = "dev"
  resource_group_name          = azurerm_resource_group.dev.name
  location                     = azurerm_resource_group.dev.location
  service_plan_name            = "ASP-Queenzone-Dev"
  web_app_name                 = "queenzone-devbox"
  log_analytics_workspace_name = "queenzone-devbox-law"
  application_insights_name    = "queenzone-devbox-ai"
  sku_name                     = "B1"
  worker_count                 = 1
  custom_hostnames             = {}
  managed_hostnames            = var.enable_custom_domain ? ["dev.queenzone.org"] : []
  allow_direct_access          = !var.enable_custom_domain
}
