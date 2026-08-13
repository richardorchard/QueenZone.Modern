variable "resource_group_name" {
  description = "Existing QueenZone production resource group."
  type        = string
  default     = "Queenzone-RG"
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
