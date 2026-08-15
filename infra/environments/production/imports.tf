locals {
  azure_resource_group_id = "/subscriptions/${var.azure_subscription_id}/resourceGroups/${var.azure_resource_group_name}"
  azure_web_base_id       = "${local.azure_resource_group_id}/providers/Microsoft.Web"
  azure_monitor_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.Insights"
  log_analytics_base_id   = "${local.azure_resource_group_id}/providers/Microsoft.OperationalInsights"
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
