variable "resource_group_id" {
  description = "Existing QueenZone production resource group ID."
  type        = string
}

variable "location" {
  description = "Azure region for low-cost mobile build storage."
  type        = string
  default     = "australiaeast"
}

variable "storage_account_name" {
  description = "Dedicated mobile test-build Storage account name."
  type        = string
  default     = "queenzonemobilebuilds"
}

variable "publisher_principal_id" {
  description = "Object ID of the GitHub deploy OIDC service principal."
  type        = string
  default     = "bb1dfbf7-851d-474b-8749-2f692e2f8f36"
}
