terraform {
  required_version = "= 1.12.5"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "= 5.0.1"
    }

    azapi = {
      source  = "Azure/azapi"
      version = "= 2.11.0"
    }

    cloudflare = {
      source  = "cloudflare/cloudflare"
      version = "= 5.23.0"
    }
  }

  backend "azurerm" {}
}
