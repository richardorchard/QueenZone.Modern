resource "azapi_resource" "sql_server" {
  type      = "Microsoft.Sql/servers@2025-02-01-preview"
  name      = var.sql_server_name
  parent_id = var.resource_group_id
  location  = var.location

  body = {
    properties = {
      # ARM requires the existing server administrator name in the resource
      # shape. Its password remains outside OpenTofu and is never exported.
      administratorLogin = "CloudSA6f234939"
      administrators = {
        administratorType         = "ActiveDirectory"
        azureADOnlyAuthentication = false
        login                     = "richard@thinkingwebsites.com.au"
        principalType             = "User"
        sid                       = "95e57126-c09d-4737-a76b-8281fac1ad6d"
        tenantId                  = "c9f094fd-23bf-4a35-a406-bcaacd7e1a8e"
      }
      minimalTlsVersion             = "1.2"
      publicNetworkAccess           = "Enabled"
      retentionDays                 = -1
      restrictOutboundNetworkAccess = "Disabled"
      version                       = "12.0"
    }
  }

  response_export_values = []

  lifecycle {
    prevent_destroy = true

    # The Entra administrator is recorded to preserve the existing ARM shape,
    # but operator identity rotation is governed outside this stack.
    ignore_changes = [
      body.properties.administrators,
      identity,
    ]
  }
}

resource "azurerm_mssql_firewall_rule" "azure_services" {
  name             = "AllowAllWindowsAzureIps"
  server_id        = azapi_resource.sql_server.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_mssql_database" "production" {
  name      = var.sql_database_name
  server_id = azapi_resource.sql_server.id

  sku_name                            = "Basic"
  max_size_gb                         = 2
  collation                           = "SQL_Latin1_General_CP1_CI_AS"
  storage_account_type                = "Local"
  zone_redundant                      = false
  read_scale                          = false
  ledger_enabled                      = false
  transparent_data_encryption_enabled = true

  short_term_retention_policy {
    retention_days           = 7
    backup_interval_in_hours = 24
  }

  long_term_retention_policy {
    weekly_retention  = "PT0S"
    monthly_retention = "PT0S"
    yearly_retention  = "PT0S"
    week_of_year      = 1
  }

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_mssql_server_extended_auditing_policy" "production" {
  server_id              = azapi_resource.sql_server.id
  enabled                = false
  log_monitoring_enabled = false
  retention_in_days      = 0

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_mssql_database_extended_auditing_policy" "production" {
  database_id            = azurerm_mssql_database.production.id
  enabled                = false
  log_monitoring_enabled = false
  retention_in_days      = 0

  lifecycle {
    prevent_destroy = true
  }
}

# AzAPI manages Storage through ARM without calling listKeys. The AzureRM
# storage-account resource exports keys and connection strings into state,
# which violates this stack's no-secrets-in-state boundary.
resource "azapi_resource" "storage_account" {
  type      = "Microsoft.Storage/storageAccounts@2026-04-01"
  name      = var.storage_account_name
  parent_id = var.resource_group_id
  location  = var.location

  body = {
    kind = "StorageV2"
    sku = {
      name = "Standard_LRS"
    }
    properties = {
      accessTier                   = "Hot"
      allowBlobPublicAccess        = true
      allowCrossTenantReplication  = false
      allowSharedKeyAccess         = true
      defaultToOAuthAuthentication = false
      dnsEndpointType              = "Standard"
      dualStackEndpointPreference = {
        publishIpv6Endpoint = false
      }
      minimumTlsVersion        = "TLS1_2"
      publicNetworkAccess      = "Enabled"
      supportsHttpsTrafficOnly = true
      customDomain = {
        name = "cdn.queenzone.org"
      }
      encryption = {
        keySource                       = "Microsoft.Storage"
        requireInfrastructureEncryption = false
        services = {
          blob = {
            enabled = true
            keyType = "Account"
          }
          file = {
            enabled = true
            keyType = "Account"
          }
        }
      }
      networkAcls = {
        bypass              = "AzureServices"
        defaultAction       = "Allow"
        ipRules             = []
        ipv6Rules           = []
        virtualNetworkRules = []
      }
    }
  }

  response_export_values = []

  lifecycle {
    prevent_destroy = true
  }
}

resource "azapi_resource" "blob_service" {
  type      = "Microsoft.Storage/storageAccounts/blobServices@2026-04-01"
  name      = "default"
  parent_id = azapi_resource.storage_account.id

  body = {
    properties = {
      containerDeleteRetentionPolicy = {
        days    = 7
        enabled = true
      }
      cors = {
        corsRules = []
      }
      deleteRetentionPolicy = {
        allowPermanentDelete = false
        days                 = 7
        enabled              = true
      }
      staticWebsite = {
        enabled = false
      }
    }
  }

  response_export_values = []

  lifecycle {
    prevent_destroy = true
  }
}

resource "azapi_resource" "container" {
  for_each = var.containers

  type      = "Microsoft.Storage/storageAccounts/blobServices/containers@2026-04-01"
  name      = each.key
  parent_id = azapi_resource.blob_service.id

  body = {
    properties = {
      defaultEncryptionScope      = "$account-encryption-key"
      denyEncryptionScopeOverride = false
      publicAccess                = each.value
    }
  }

  response_export_values = []

  lifecycle {
    prevent_destroy = true
  }
}
