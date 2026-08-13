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

variable "service_plan_name" {
  description = "Existing App Service plan name."
  type        = string
  default     = "ASP-Queenzone"
}

variable "web_app_name" {
  description = "Existing Linux web app name."
  type        = string
  default     = "queenzone-dev"
}

variable "sku_name" {
  description = "Existing App Service SKU."
  type        = string
  default     = "B1"

  validation {
    condition     = var.sku_name == "B1"
    error_message = "The accepted production hosting SKU is B1."
  }
}

variable "worker_count" {
  description = "Existing App Service worker count."
  type        = number
  default     = 1

  validation {
    condition     = var.worker_count == 1
    error_message = "The accepted production topology is single-instance."
  }
}
