output "web_app_id" {
  value = module.azure_web.web_app_id
}

output "default_hostname" {
  value = module.azure_web.default_hostname
}

output "custom_domain_verification_id" {
  sensitive = true
  value     = module.azure_web.custom_domain_verification_id
}

output "managed_identity_principal_id" {
  value = module.azure_web.managed_identity_principal_id
}
