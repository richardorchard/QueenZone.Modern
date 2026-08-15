output "import_contract" {
  description = "Non-sensitive names that later Azure web imports must match."
  value = {
    resource_group = var.resource_group_name
    service_plan   = var.service_plan_name
    web_app        = var.web_app_name
    log_analytics  = var.log_analytics_workspace_name
    app_insights   = var.application_insights_name
    hostnames      = sort(keys(var.custom_hostnames))
  }
}

output "web_app_id" {
  description = "Managed production web app resource ID."
  value       = azurerm_linux_web_app.production.id
}

output "managed_identity_principal_id" {
  description = "System-assigned identity principal ID for separately scoped RBAC."
  value       = azurerm_linux_web_app.production.identity[0].principal_id
}
