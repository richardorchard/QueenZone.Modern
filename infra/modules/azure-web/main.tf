locals {
  cloudflare_ip_restrictions = [
    {
      name        = "Cloudflare-IPv4-1"
      priority    = 100
      ip_address  = "173.245.48.0/20,103.21.244.0/22,103.22.200.0/22,103.31.4.0/22,141.101.64.0/18,108.162.192.0/18,190.93.240.0/20,188.114.96.0/20"
      description = "Allow Cloudflare published IPv4 ranges 1/2"
    },
    {
      name        = "Cloudflare-IPv4-2"
      priority    = 110
      ip_address  = "197.234.240.0/22,198.41.128.0/17,162.158.0.0/15,104.16.0.0/13,104.24.0.0/14,172.64.0.0/13,131.0.72.0/22"
      description = "Allow Cloudflare published IPv4 ranges 2/2"
    },
    {
      name        = "Cloudflare-IPv6"
      priority    = 120
      ip_address  = "2400:cb00::/32,2606:4700::/32,2803:f800::/32,2405:b500::/32,2405:8100::/32,2a06:98c0::/29,2c0f:f248::/32"
      description = "Allow Cloudflare published IPv6 ranges"
    },
  ]
}

resource "azurerm_log_analytics_workspace" "production" {
  name                = var.log_analytics_workspace_name
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = 0.1

  allow_resource_only_permissions = true
  internet_ingestion_access_type  = "Enabled"
  internet_query_access_type      = "Enabled"

  lifecycle {
    prevent_destroy = true

    # Azure reports this legacy workspace flag as null while the provider
    # normalises an omitted value to true. Preserve the imported ARM shape.
    ignore_changes = [local_authentication_enabled]
  }
}

resource "azurerm_application_insights" "production" {
  name                = var.application_insights_name
  location            = var.location
  resource_group_name = var.resource_group_name
  workspace_id        = azurerm_log_analytics_workspace.production.id
  application_type    = "web"
  retention_in_days   = 90

  internet_ingestion_enabled = true
  internet_query_enabled     = true
  sampling_percentage        = 0

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_service_plan" "production" {
  name                = var.service_plan_name
  location            = var.location
  resource_group_name = var.resource_group_name
  os_type             = "Linux"
  sku_name            = var.sku_name
  worker_count        = var.worker_count

  per_site_scaling_enabled = false
  zone_balancing_enabled   = false

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_linux_web_app" "production" {
  name                = var.web_app_name
  location            = var.location
  resource_group_name = var.resource_group_name
  service_plan_id     = azurerm_service_plan.production.id

  enabled                    = true
  https_only                 = true
  client_affinity_enabled    = false
  client_certificate_enabled = false

  tags = {
    "hidden-link: /app-insights-resource-id" = replace(
      azurerm_application_insights.production.id,
      "Microsoft.Insights",
      "microsoft.insights",
    )
  }

  identity {
    type = "SystemAssigned"
  }

  site_config {
    always_on                         = true
    ftps_state                        = "FtpsOnly"
    health_check_path                 = "/health"
    health_check_eviction_time_in_min = 10
    http2_enabled                     = true
    minimum_tls_version               = "1.2"
    remote_debugging_enabled          = false
    scm_minimum_tls_version           = "1.2"
    scm_use_main_ip_restriction       = false
    use_32_bit_worker                 = true
    websockets_enabled                = false
    worker_count                      = var.worker_count

    application_stack {
      dotnet_version = "10.0"
    }

    dynamic "ip_restriction" {
      for_each = local.cloudflare_ip_restrictions

      content {
        name        = ip_restriction.value.name
        priority    = ip_restriction.value.priority
        action      = "Allow"
        ip_address  = ip_restriction.value.ip_address
        description = ip_restriction.value.description
      }
    }
  }

  logs {
    detailed_error_messages = false
    failed_request_tracing  = false

    application_logs {
      file_system_level = "Information"
    }

    http_logs {
      file_system {
        retention_in_days = 3
        retention_in_mb   = 100
      }
    }
  }

  lifecycle {
    prevent_destroy = true

    # #618 owns the split between ARM deploy settings and operator-managed
    # secrets. Reading this map into state would expose values; managing an
    # incomplete map would delete live settings. AzureRM also normalises the
    # live explicit main-site deny-all and SCM allow-all rules to empty default
    # actions during import, then plans unsafe/default-only rewrites. Keep the
    # imported rules authoritative until the coordinated edge work in #626.
    ignore_changes = [
      app_settings,
      site_config[0].ip_restriction_default_action,
      site_config[0].scm_ip_restriction_default_action,
    ]
  }
}

resource "azurerm_app_service_custom_hostname_binding" "production" {
  for_each = var.custom_hostnames

  hostname            = each.key
  app_service_name    = azurerm_linux_web_app.production.name
  resource_group_name = var.resource_group_name
  ssl_state           = "SniEnabled"
  thumbprint          = each.value

  lifecycle {
    prevent_destroy = true
  }
}
