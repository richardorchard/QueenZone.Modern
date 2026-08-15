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

variable "log_analytics_workspace_name" {
  description = "Existing Log Analytics workspace name."
  type        = string
  default     = "queenzone-dev-law"
}

variable "application_insights_name" {
  description = "Existing workspace-linked Application Insights component name."
  type        = string
  default     = "queenzone-dev-ai"
}

variable "custom_hostnames" {
  description = "Existing custom hostname bindings and their uploaded certificate thumbprints. Thumbprints are identifiers, not secrets."
  type        = map(string)
  default = {
    "queenzone.org"     = "4B8832D7F69444E881F5BFF26AEC0578A415E275"
    "www.queenzone.org" = "A45C7DDFDA8719399B772B1C6336F8B28BD80B24"
  }

  validation {
    condition = (
      length(setsubtract(toset(keys(var.custom_hostnames)), toset(["queenzone.org", "www.queenzone.org"]))) == 0 &&
      length(setsubtract(toset(["queenzone.org", "www.queenzone.org"]), toset(keys(var.custom_hostnames)))) == 0
    )
    error_message = "Production must retain the apex and www custom hostnames."
  }
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
