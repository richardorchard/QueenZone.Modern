variable "account_id" {
  description = "Existing Cloudflare account ID."
  type        = string
  default     = "f93121b2086286e79a7a9fdb8d03cb4c"
}

variable "zone_id" {
  description = "Existing queenzone.org zone ID."
  type        = string
  default     = "079fc2f37095c82fb3a2b4da65718b2b"
}

variable "zone_name" {
  description = "Existing Cloudflare zone name."
  type        = string
  default     = "queenzone.org"

  validation {
    condition     = var.zone_name == "queenzone.org"
    error_message = "This module may manage only queenzone.org."
  }
}

variable "worker_name" {
  description = "Existing historical Worker name for the cdn2 media proxy."
  type        = string
  default     = "pictures-queenzone-org"

  validation {
    condition     = var.worker_name == "pictures-queenzone-org"
    error_message = "The cdn2 media Worker name is historical and must not be renamed."
  }
}

variable "worker_route" {
  description = "Existing Worker route. The media Worker belongs on cdn2 only."
  type        = string
  default     = "cdn2.queenzone.org/*"

  validation {
    condition     = var.worker_route == "cdn2.queenzone.org/*"
    error_message = "The pictures-queenzone-org Worker route must remain on cdn2 only."
  }
}

variable "legacy_worker_name" {
  description = "Existing compatibility Worker for pictures.queenzone.org."
  type        = string
  default     = "pictures-legacy-redirect"

  validation {
    condition     = var.legacy_worker_name == "pictures-legacy-redirect"
    error_message = "The pictures-legacy-redirect Worker name must not be renamed."
  }
}

variable "legacy_worker_route" {
  description = "Existing compatibility Worker route for the retired pictures hostname."
  type        = string
  default     = "pictures.queenzone.org/*"

  validation {
    condition     = var.legacy_worker_route == "pictures.queenzone.org/*"
    error_message = "The pictures-legacy-redirect Worker route must remain on pictures.queenzone.org only."
  }
}
