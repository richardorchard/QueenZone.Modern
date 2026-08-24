data "cloudflare_ip_ranges" "current" {}

locals {
  cdn2_worker_source   = "${path.module}/workers/pictures-queenzone-org.js"
  legacy_worker_source = "${path.module}/workers/pictures-legacy-redirect.js"

  # Inventory-confirmed DNS only. Do not add registrar, email, or guessed records.
  dns_records = {
    apex = {
      type    = "A"
      name    = "queenzone.org"
      content = "52.237.246.162"
      proxied = true
      ttl     = 1
      comment = null
    }
    www = {
      type    = "CNAME"
      name    = "www.queenzone.org"
      content = "queenzone-dev.azurewebsites.net"
      proxied = true
      ttl     = 1
      comment = null
    }
    dev = {
      type    = "CNAME"
      name    = "dev.queenzone.org"
      content = "lively-mushroom-017550800.7.azurestaticapps.net"
      proxied = false
      ttl     = 1
      comment = "Azure Static Web App mobile test builds; issue #809"
    }
    cdn = {
      type    = "CNAME"
      name    = "cdn.queenzone.org"
      content = "queenzone.blob.core.windows.net"
      proxied = true
      ttl     = 1
      comment = null
    }
    cdn2 = {
      type    = "CNAME"
      name    = "cdn2.queenzone.org"
      content = "queenzone.blob.core.windows.net"
      proxied = true
      ttl     = 1
      comment = null
    }
    pictures = {
      type    = "CNAME"
      name    = "pictures.queenzone.org"
      content = "cdn.queenzone.org"
      proxied = true
      ttl     = 1
      comment = "Legacy media hostname compatibility for Search Console and old links"
    }
    asverify_cdn = {
      type    = "CNAME"
      name    = "asverify.cdn.queenzone.org"
      content = "asverify.queenzone.blob.core.windows.net"
      proxied = false
      ttl     = 1
      comment = null
    }
    asuid = {
      type    = "TXT"
      name    = "asuid.queenzone.org"
      content = "\"0740A9DBFCBF1CE22090C2574D9867A086204B58D7CE4676D90FEB5E85B3A068\""
      proxied = false
      ttl     = 1
      comment = null
    }
    asuid_www = {
      type    = "TXT"
      name    = "asuid.www.queenzone.org"
      content = "\"0740A9DBFCBF1CE22090C2574D9867A086204B58D7CE4676D90FEB5E85B3A068\""
      proxied = false
      ttl     = 1
      comment = null
    }
    google_site = {
      type    = "TXT"
      name    = "queenzone.org"
      content = "\"google-site-verification=gDSLwU-DDTXw-Z3VYtqy0xZrLPcqqfdX4VZkA-9Ttbg\""
      proxied = false
      ttl     = 3600
      comment = null
    }
    bing_verify_1 = {
      type    = "CNAME"
      name    = "809286c091b4aeaedf6cd9c90b65a333.queenzone.org"
      content = "verify.bing.com"
      proxied = false
      ttl     = 1
      comment = null
    }
    bing_verify_2 = {
      type    = "CNAME"
      name    = "a0151149b4c5e0d0ac33d250b9dd6db4.queenzone.org"
      content = "verify.bing.com"
      proxied = false
      ttl     = 1
      comment = null
    }
  }

  string_zone_settings = {
    ssl                      = "strict"
    always_use_https         = "on"
    tls_1_3                  = "on"
    automatic_https_rewrites = "on"
    security_level           = "medium"
    cache_level              = "aggressive"
    browser_check            = "on"
    brotli                   = "on"
    http2                    = "on"
    http3                    = "on"
    websockets               = "on"
    polish                   = "off"
    hotlink_protection       = "off"
    development_mode         = "off"
  }
}

resource "cloudflare_zone" "production" {
  account = {
    id = var.account_id
  }
  name = var.zone_name
  type = "full"

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_dns_record" "this" {
  for_each = local.dns_records

  zone_id = var.zone_id
  name    = each.value.name
  type    = each.value.type
  content = each.value.content
  ttl     = each.value.ttl
  proxied = each.value.proxied
  comment = each.value.comment

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_zone_setting" "this" {
  for_each = local.string_zone_settings

  zone_id    = var.zone_id
  setting_id = each.key
  value      = each.value
}

resource "cloudflare_zone_setting" "challenge_ttl" {
  zone_id    = var.zone_id
  setting_id = "challenge_ttl"
  value      = 1800
}

resource "cloudflare_workers_script" "cdn2" {
  account_id  = var.account_id
  script_name = var.worker_name
  content     = file(local.cdn2_worker_source)

  lifecycle {
    prevent_destroy = true

    # Provider 5.23 reports a content rewrite and computed metadata on the
    # first import even when the script body matches live. Ignoring those
    # attributes keeps the first apply from republishing cdn2. After import
    # is in state, a refresh-only plan must stay no-op before content is
    # removed from this list.
    ignore_changes = [
      annotations,
      bindings,
      compatibility_date,
      compatibility_flags,
      content,
      keep_bindings,
      limits,
      logpush,
      observability,
      placement,
      usage_model,
    ]
  }
}

resource "cloudflare_workers_script" "legacy_pictures" {
  account_id  = var.account_id
  script_name = var.legacy_worker_name
  content     = file(local.legacy_worker_source)

  lifecycle {
    prevent_destroy = true

    ignore_changes = [
      annotations,
      bindings,
      compatibility_date,
      compatibility_flags,
      content,
      keep_bindings,
      limits,
      logpush,
      observability,
      placement,
      usage_model,
    ]
  }
}

resource "cloudflare_workers_route" "cdn2" {
  zone_id = var.zone_id
  pattern = var.worker_route
  script  = cloudflare_workers_script.cdn2.script_name

  lifecycle {
    prevent_destroy = true
  }
}

resource "cloudflare_workers_route" "legacy_pictures" {
  zone_id = var.zone_id
  pattern = var.legacy_worker_route
  script  = cloudflare_workers_script.legacy_pictures.script_name

  lifecycle {
    prevent_destroy = true
  }
}

check "cdn_has_no_worker_route" {
  assert {
    condition = alltrue([
      cloudflare_workers_route.cdn2.pattern != "cdn.queenzone.org/*",
      cloudflare_workers_route.legacy_pictures.pattern != "cdn.queenzone.org/*",
    ])
    error_message = "cdn.queenzone.org must remain a straight Cloudflare proxy with no Worker route."
  }
}

check "cdn2_worker_route_retained" {
  assert {
    condition     = cloudflare_workers_route.cdn2.pattern == "cdn2.queenzone.org/*" && cloudflare_workers_route.cdn2.script == "pictures-queenzone-org"
    error_message = "The pictures-queenzone-org Worker must stay on cdn2.queenzone.org/*."
  }
}
