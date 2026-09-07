mock_provider "azapi" {}
mock_provider "azurerm" {
  mock_resource "azurerm_mssql_server" {
    defaults = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Sql/servers/queenzone-prod-sql"
    }
  }
}

variables {
  resource_group_id                    = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test"
  resource_group_name                  = "test"
  sql_server_administrator_password_wo = "test-only-password"
}

run "existing_production_shape_remains_managed" {
  command = plan

  override_resource {
    target = azapi_resource.sql_server
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Sql/servers/test"
    }
  }

  override_resource {
    target = azapi_resource.storage_account
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/test"
    }
  }

  override_resource {
    target = azapi_resource.blob_service
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/test/blobServices/default"
    }
  }

  override_resource {
    target = azurerm_mssql_database.production
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Sql/servers/test/databases/queenzone-db"
    }
  }

  assert {
    condition     = length(azapi_resource.sql_server) == 1 && length(azurerm_mssql_server.created) == 0 && length(azurerm_mssql_database.production) == 1
    error_message = "The default imported production shape must retain its AzAPI server and managed database."
  }
}

run "migration_target_uses_write_only_password_and_defers_database" {
  command = plan

  override_resource {
    target = azapi_resource.storage_account
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/queenzoneprod"
    }
  }


  override_resource {
    target = azapi_resource.blob_service
    values = {
      id = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/test/providers/Microsoft.Storage/storageAccounts/queenzoneprod/blobServices/default"
    }
  }

  variables {
    create_sql_server_with_write_only_password = true
    manage_sql_database                        = false
    sql_server_name                            = "queenzone-prod-sql"
    storage_account_name                       = "queenzoneprod"
  }

  assert {
    condition     = length(azapi_resource.sql_server) == 0 && length(azurerm_mssql_server.created) == 1 && length(azurerm_mssql_database.production) == 0
    error_message = "The migration target must create its server with the write-only password and defer the database until Azure copy completes."
  }
}
