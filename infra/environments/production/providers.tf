provider "azurerm" {
  resource_provider_registrations = "none"

  features {
    resource_group {
      prevent_deletion_if_contains_resources = true
    }
  }
}

provider "cloudflare" {
  # Reads CLOUDFLARE_API_TOKEN from the process environment.
}
