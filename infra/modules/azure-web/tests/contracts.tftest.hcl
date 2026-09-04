mock_provider "azurerm" {
  mock_resource "azurerm_app_service_custom_hostname_binding" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Web/sites/test/hostNameBindings/dev.queenzone.org"
    }
  }
  mock_resource "azurerm_app_service_managed_certificate" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Web/certificates/test"
    }
  }
  mock_resource "azurerm_log_analytics_workspace" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.OperationalInsights/workspaces/test"
    }
  }
  mock_resource "azurerm_service_plan" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Web/serverFarms/test"
    }
  }
}

run "production_defaults" {
  command = plan
  assert {
    condition     = azurerm_linux_web_app.production.site_config[0].ip_restriction_default_action == "Deny" && length(azurerm_app_service_custom_hostname_binding.production) == 2 && length(azurerm_app_service_managed_certificate.managed) == 0
    error_message = "Production must preserve Cloudflare ingress and both uploaded TLS bindings."
  }
  assert {
    condition     = azurerm_service_plan.production.sku_name == "B1" && azurerm_service_plan.production.worker_count == 1 && azurerm_linux_web_app.production.site_config[0].always_on
    error_message = "Always-on single-worker B1 must be preserved."
  }
}

run "dev_bootstrap" {
  command = plan
  variables {
    environment_name    = "dev"
    custom_hostnames    = {}
    allow_direct_access = true
  }
  assert {
    condition     = azurerm_linux_web_app.production.site_config[0].ip_restriction_default_action == "Allow" && length(azurerm_app_service_custom_hostname_binding.production) == 0 && length(azurerm_app_service_custom_hostname_binding.managed) == 0
    error_message = "Dev must be reachable before DNS and create no premature bindings."
  }
  assert {
    condition     = azurerm_linux_web_app.production.app_settings["WEBSITE_WARMUP_PATH"] == "/health"
    error_message = "Fresh dev apps need a warmup setting."
  }
}

run "production_rejects_direct_access" {
  command = plan
  variables {
    allow_direct_access = true
  }
  expect_failures = [var.allow_direct_access]
}

run "production_retains_hostnames" {
  command = plan
  variables {
    custom_hostnames = {}
  }
  expect_failures = [var.custom_hostnames]
}

run "production_rejects_managed_dev_hostname" {
  command = plan
  variables {
    managed_hostnames = ["dev.queenzone.org"]
  }
  expect_failures = [var.managed_hostnames]
}

run "dev_rejects_production_hostname" {
  command = plan
  variables {
    environment_name = "dev"
    custom_hostnames = { "www.queenzone.org" = "thumbprint" }
  }
  expect_failures = [var.custom_hostnames]
}

run "dev_managed_tls_after_dns" {
  command = plan
  variables {
    environment_name  = "dev"
    custom_hostnames  = {}
    managed_hostnames = ["dev.queenzone.org"]
  }
  assert {
    condition     = length(azurerm_app_service_managed_certificate.managed) == 1 && azurerm_app_service_certificate_binding.managed["dev.queenzone.org"].ssl_state == "SniEnabled" && azurerm_linux_web_app.production.site_config[0].ip_restriction_default_action == "Deny"
    error_message = "The later DNS phase must enable managed SNI TLS and restricted ingress."
  }
}
