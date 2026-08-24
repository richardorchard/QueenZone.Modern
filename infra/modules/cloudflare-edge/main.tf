resource "cloudflare_zone" "queenzone" {
  account = {
    id = var.account_id
  }
  name   = var.zone_name
  type   = "full"
  paused = false

  lifecycle {
    prevent_destroy = true
  }
}

# TLS/security/cache zone settings confirmed live during the #624 audit
# (infra/import/cloudflare-hostnames.json). min_tls_version is deliberately
# left unmanaged: it is a dashboard default (1.0), not a reviewed decision.
resource "cloudflare_zone_setting" "ssl" {
  zone_id    = var.zone_id
  setting_id = "ssl"
  value      = "strict"
}

resource "cloudflare_zone_setting" "tls_1_3" {
  zone_id    = var.zone_id
  setting_id = "tls_1_3"
  value      = "on"
}

resource "cloudflare_zone_setting" "always_use_https" {
  zone_id    = var.zone_id
  setting_id = "always_use_https"
  value      = "on"
}

resource "cloudflare_zone_setting" "automatic_https_rewrites" {
  zone_id    = var.zone_id
  setting_id = "automatic_https_rewrites"
  value      = "on"
}

resource "cloudflare_zone_setting" "security_level" {
  zone_id    = var.zone_id
  setting_id = "security_level"
  value      = "medium"
}

resource "cloudflare_zone_setting" "cache_level" {
  zone_id    = var.zone_id
  setting_id = "cache_level"
  value      = "aggressive"
}

resource "cloudflare_zone_setting" "browser_check" {
  zone_id    = var.zone_id
  setting_id = "browser_check"
  value      = "on"
}

resource "cloudflare_zone_setting" "challenge_ttl" {
  zone_id    = var.zone_id
  setting_id = "challenge_ttl"
  value      = 1800
}

resource "cloudflare_zone_setting" "brotli" {
  zone_id    = var.zone_id
  setting_id = "brotli"
  value      = "on"
}

resource "cloudflare_zone_setting" "http2" {
  zone_id    = var.zone_id
  setting_id = "http2"
  value      = "on"
}

resource "cloudflare_zone_setting" "http3" {
  zone_id    = var.zone_id
  setting_id = "http3"
  value      = "on"
}

resource "cloudflare_zone_setting" "websockets" {
  zone_id    = var.zone_id
  setting_id = "websockets"
  value      = "on"
}

resource "cloudflare_zone_setting" "polish" {
  zone_id    = var.zone_id
  setting_id = "polish"
  value      = "off"
}

resource "cloudflare_zone_setting" "hotlink_protection" {
  zone_id    = var.zone_id
  setting_id = "hotlink_protection"
  value      = "off"
}

resource "cloudflare_zone_setting" "development_mode" {
  zone_id    = var.zone_id
  setting_id = "development_mode"
  value      = "off"
}

# --- DNS records (infra/import/cloudflare-hostnames.json, #624 audit) ---

resource "cloudflare_dns_record" "apex" {
  zone_id = var.zone_id
  name    = "queenzone.org"
  type    = "A"
  content = "52.237.246.162"
  ttl     = 1
  proxied = true
  comment = "Apex -> App Service inbound IP"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "www" {
  zone_id = var.zone_id
  name    = "www.queenzone.org"
  type    = "CNAME"
  content = "queenzone-dev.azurewebsites.net"
  ttl     = 1
  proxied = true

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "dev" {
  zone_id = var.zone_id
  name    = "dev.queenzone.org"
  type    = "CNAME"
  content = "lively-mushroom-017550800.7.azurestaticapps.net"
  ttl     = 1
  proxied = false
  comment = "Azure Static Web App mobile test-build downloads (#809)"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "cdn" {
  zone_id = var.zone_id
  name    = "cdn.queenzone.org"
  type    = "CNAME"
  content = "queenzone.blob.core.windows.net"
  ttl     = 1
  proxied = true
  comment = "Straight proxy to Azure Blob; no Worker route"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "cdn2" {
  zone_id = var.zone_id
  name    = "cdn2.queenzone.org"
  type    = "CNAME"
  content = "queenzone.blob.core.windows.net"
  ttl     = 1
  proxied = true
  comment = "Worker route cdn2.queenzone.org/* fronts this record"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "pictures_legacy" {
  zone_id = var.zone_id
  name    = "pictures.queenzone.org"
  type    = "CNAME"
  content = "cdn.queenzone.org"
  ttl     = 1
  proxied = true
  comment = "Retired media hostname compatibility"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "asverify_cdn" {
  zone_id = var.zone_id
  name    = "asverify.cdn.queenzone.org"
  type    = "CNAME"
  content = "asverify.queenzone.blob.core.windows.net"
  ttl     = 1
  proxied = false
  comment = "Azure Storage custom-domain verification"

  lifecycle {
    prevent_destroy = true
  }
}

# Verification TXT records: the #624 audit recorded that these exist and are
# non-secret, but did not capture their content values. Import reads the live
# content; ignore_changes keeps this module from ever proposing to overwrite
# an unrecorded verification token.
resource "cloudflare_dns_record" "asuid_apex" {
  zone_id = var.zone_id
  name    = "asuid.queenzone.org"
  type    = "TXT"
  content = "imported"
  ttl     = 1
  comment = "App Service custom domain verification"

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [content]
  }
}

resource "cloudflare_dns_record" "asuid_www" {
  zone_id = var.zone_id
  name    = "asuid.www.queenzone.org"
  type    = "TXT"
  content = "imported"
  ttl     = 1
  comment = "App Service custom domain verification"

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [content]
  }
}

resource "cloudflare_dns_record" "google_site_verification" {
  zone_id = var.zone_id
  name    = "queenzone.org"
  type    = "TXT"
  content = "imported"
  ttl     = 1
  comment = "Google site verification"

  lifecycle {
    prevent_destroy = true
    ignore_changes  = [content]
  }
}

resource "cloudflare_dns_record" "bing_verification_1" {
  zone_id = var.zone_id
  name    = "809286c091b4aeaedf6cd9c90b65a333.queenzone.org"
  type    = "CNAME"
  content = "verify.bing.com"
  ttl     = 1
  proxied = false
  comment = "Bing verification"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "bing_verification_2" {
  zone_id = var.zone_id
  name    = "a0151149b4c5e0d0ac33d250b9dd6db4.queenzone.org"
  type    = "CNAME"
  content = "verify.bing.com"
  ttl     = 1
  proxied = false
  comment = "Bing verification"

  lifecycle {
    prevent_destroy = true
  }
}

# --- Workers ---

resource "cloudflare_workers_script" "pictures_queenzone_org" {
  account_id  = var.account_id
  script_name = var.worker_name
  content     = file("${path.module}/../../import/workers/pictures-queenzone-org.js")

  lifecycle {
    prevent_destroy = true

    # Runtime knobs (compatibility date/flags, usage model, bindings) were not
    # captured during the #624 audit. Ignoring them keeps this module from
    # guessing at values that could change published Worker behaviour.
    ignore_changes = [
      compatibility_date,
      compatibility_flags,
      usage_model,
      bindings,
      observability,
      migrations,
      assets,
      main_module,
      body_part,
    ]
  }
}

resource "cloudflare_workers_script" "pictures_legacy_redirect" {
  account_id  = var.account_id
  script_name = var.legacy_redirect_worker_name
  content     = file("${path.module}/../../import/workers/pictures-legacy-redirect.js")

  lifecycle {
    prevent_destroy = true

    ignore_changes = [
      compatibility_date,
      compatibility_flags,
      usage_model,
      bindings,
      observability,
      migrations,
      assets,
      main_module,
      body_part,
    ]
  }
}

resource "cloudflare_workers_route" "cdn2_media" {
  zone_id = var.zone_id
  pattern = var.worker_route
  script  = cloudflare_workers_script.pictures_queenzone_org.script_name

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_workers_route" "pictures_legacy_redirect" {
  zone_id = var.zone_id
  pattern = var.legacy_redirect_route
  script  = cloudflare_workers_script.pictures_legacy_redirect.script_name

  lifecycle {
    prevent_destroy = true
  }
}
