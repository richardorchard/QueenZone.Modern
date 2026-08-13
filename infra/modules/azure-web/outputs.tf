output "import_contract" {
  description = "Non-sensitive names that later Azure web imports must match."
  value = {
    resource_group = var.resource_group_name
    service_plan   = var.service_plan_name
    web_app        = var.web_app_name
  }
}
