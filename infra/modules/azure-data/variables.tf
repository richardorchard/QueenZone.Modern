variable "resource_group_name" {
  description = "Existing QueenZone production resource group."
  type        = string
  default     = "Queenzone-RG"
}

variable "resource_group_id" {
  description = "Existing QueenZone production resource group ID."
  type        = string
}

variable "location" {
  description = "Existing Azure region."
  type        = string
  default     = "australiaeast"
}

variable "sql_server_name" {
  description = "Existing Azure SQL logical server name."
  type        = string
  default     = "queenzone-sql-server"
}

variable "sql_database_name" {
  description = "Existing Azure SQL database name."
  type        = string
  default     = "queenzone-db"
}

variable "storage_account_name" {
  description = "Existing application media storage account name."
  type        = string
  default     = "queenzone"
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
