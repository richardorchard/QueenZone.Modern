variable "resource_group_name" {
  description = "Resource group that owns the storage account."
  type        = string
  default     = "Queenzone-RG"
}

variable "resource_group_id" {
  description = "Resource group ID that owns the storage account."
  type        = string
}

variable "location" {
  description = "Azure region for the storage account."
  type        = string
  default     = "australiaeast"
}

variable "sql_server_name" {
  description = "Azure SQL logical server name when this module manages the server."
  type        = string
  default     = "queenzone-sql-server"
}

variable "sql_database_name" {
  description = "Azure SQL database name."
  type        = string
  default     = "queenzone-db"
}

variable "storage_account_name" {
  description = "Application media storage account name."
  type        = string
  default     = "queenzone"
}

variable "sql_database_sku_name" {
  description = "Azure SQL database SKU name."
  type        = string
  default     = "S0"
}

variable "sql_database_max_size_gb" {
  description = "Maximum Azure SQL database size in GB."
  type        = number
  default     = 10

  validation {
    condition     = var.sql_database_max_size_gb >= 2
    error_message = "sql_database_max_size_gb must be at least 2 GB."
  }
}

variable "existing_sql_server_id" {
  description = "Existing Azure SQL logical server ARM ID. When set, this module creates only the database on that server."
  type        = string
  default     = null
  nullable    = true

  validation {
    condition     = var.existing_sql_server_id == null || can(regex("^/subscriptions/[0-9a-f-]+/resourceGroups/[^/]+/providers/Microsoft\\.Sql/servers/[^/]+$", var.existing_sql_server_id))
    error_message = "existing_sql_server_id must be an Azure SQL logical server ARM ID."
  }
}

variable "create_azure_services_firewall_rule" {
  description = "Whether this module owns the server-wide AllowAllWindowsAzureIps firewall rule."
  type        = bool
  default     = true

  validation {
    condition     = var.existing_sql_server_id == null || !var.create_azure_services_firewall_rule
    error_message = "A caller using an existing SQL server must not create a server-wide firewall rule."
  }
}

variable "create_server_extended_auditing_policy" {
  description = "Whether this module owns the SQL server-wide auditing policy."
  type        = bool
  default     = true

  validation {
    condition     = var.existing_sql_server_id == null || !var.create_server_extended_auditing_policy
    error_message = "A caller using an existing SQL server must not create its server-wide auditing policy."
  }
}

variable "storage_custom_domain_name" {
  description = "Optional custom domain for the storage account."
  type        = string
  default     = "cdn.queenzone.org"
  nullable    = true
}

variable "manage_blob_service" {
  description = "Whether this module manages blob-service settings instead of using Azure's automatic default service."
  type        = bool
  default     = true
}

variable "containers" {
  description = "Live Blob container ACLs approved for import. None means private; test and missing future UGC containers are excluded."
  type        = map(string)
  default = {
    "album-or-single-covers"  = "Blob"
    "attachments"             = "Blob"
    "avatars"                 = "Blob"
    "brian-may"               = "Blob"
    "css"                     = "Container"
    "databasebackup"          = "None"
    "fan-art"                 = "Blob"
    "fan-pics"                = "Blob"
    "forum"                   = "Blob"
    "freddie-mercury"         = "Blob"
    "freddie-tribute-concert" = "Blob"
    "images"                  = "Blob"
    "john-deacon"             = "Blob"
    "miscellaneous"           = "Blob"
    "mp3"                     = "Blob"
    "pre-queen"               = "Blob"
    "queen"                   = "Blob"
    "queen-and-adam-lambert"  = "Blob"
    "queen-and-paul-rodgers"  = "Blob"
    "queen-memorabillia"      = "Blob"
    "roger-taylor"            = "Blob"
    "songfiles"               = "None"
    "special-events"          = "Blob"
    "ugc-avatars"             = "None"
    "ugc-forum"               = "None"
    "us-convention-2001"      = "Blob"
  }

  validation {
    condition     = alltrue([for access in values(var.containers) : contains(["None", "Blob", "Container"], access)])
    error_message = "Container access must be None, Blob, or Container."
  }

  validation {
    condition     = var.containers["databasebackup"] == "None" && var.containers["ugc-avatars"] == "None" && var.containers["ugc-forum"] == "None" && var.containers["songfiles"] == "None"
    error_message = "Backup, modern UGC, and songfiles containers must remain private."
  }
}
