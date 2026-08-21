# Dedicated throwaway-build storage. AzAPI avoids exporting account keys or
# connection strings into OpenTofu state.
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
      minimumTlsVersion            = "TLS1_2"
      publicNetworkAccess          = "Enabled"
      supportsHttpsTrafficOnly     = true
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
      cors = {
        corsRules = []
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

resource "azapi_resource" "builds_container" {
  type      = "Microsoft.Storage/storageAccounts/blobServices/containers@2026-04-01"
  name      = "builds"
  parent_id = azapi_resource.blob_service.id

  body = {
    properties = {
      defaultEncryptionScope      = "$account-encryption-key"
      denyEncryptionScopeOverride = false
      # Blob permits anonymous object reads but does not expose container listing.
      publicAccess = "Blob"
    }
  }

  response_export_values = []

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_role_assignment" "mobile_publisher" {
  scope                            = azapi_resource.storage_account.id
  role_definition_name             = "Storage Blob Data Contributor"
  principal_id                     = var.publisher_principal_id
  principal_type                   = "ServicePrincipal"
  skip_service_principal_aad_check = true
}
