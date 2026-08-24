output "import_contract" {
  description = "Non-sensitive IDs and names that Cloudflare imports must match."
  value = {
    account_id          = var.account_id
    zone_id             = var.zone_id
    zone_name           = var.zone_name
    worker_name         = var.worker_name
    worker_route        = var.worker_route
    legacy_worker_name  = var.legacy_worker_name
    legacy_worker_route = var.legacy_worker_route
    dns_record_keys     = sort(keys(local.dns_records))
  }
}

output "published_ipv4_cidrs" {
  description = "Current Cloudflare published IPv4 origin ranges."
  value       = sort(data.cloudflare_ip_ranges.current.ipv4_cidrs)
}

output "published_ipv6_cidrs" {
  description = "Current Cloudflare published IPv6 origin ranges."
  value       = sort(data.cloudflare_ip_ranges.current.ipv6_cidrs)
}

output "worker_route_patterns" {
  description = "Managed Worker route patterns. cdn.queenzone.org must not appear."
  value = sort([
    cloudflare_workers_route.cdn2.pattern,
    cloudflare_workers_route.legacy_pictures.pattern,
  ])
}

output "ssl_mode" {
  description = "Declared Full (strict) TLS mode for the zone."
  value       = cloudflare_zone_setting.this["ssl"].value
}
