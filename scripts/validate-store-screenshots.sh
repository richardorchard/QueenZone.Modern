#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ios_dir="$repo_root/docs/release/store-submission/apple/assets/screenshots/upload-ready"
android_dir="$repo_root/docs/release/store-submission/google-play/assets/screenshots/upload-ready"

validate_set() {
  local directory="$1"
  local expected_width="$2"
  local expected_height="$3"
  local maximum_count="$4"
  local label="$5"
  local count=0

  while IFS= read -r -d '' image; do
    count=$((count + 1))
    read -r width height channels <<<"$(magick identify -format '%w %h %[channels]' "$image")"
    if [[ "$width" != "$expected_width" || "$height" != "$expected_height" ]]; then
      echo "$label: invalid dimensions for $image: ${width}x${height}" >&2
      exit 1
    fi
    if [[ "$channels" == *a* ]]; then
      echo "$label: alpha channel is not allowed: $image ($channels)" >&2
      exit 1
    fi
  done < <(find "$directory" -maxdepth 1 -type f -name '*.png' -print0 | sort -z)

  if (( count == 0 || count > maximum_count )); then
    echo "$label: expected 1-$maximum_count PNG files, found $count" >&2
    exit 1
  fi
  echo "$label: $count valid ${expected_width}x${expected_height} RGB PNG files"
}

command -v magick >/dev/null || { echo 'ImageMagick (magick) is required.' >&2; exit 1; }
validate_set "$ios_dir" 1290 2796 10 "App Store iPhone"
validate_set "$android_dir" 1080 1920 8 "Google Play phone"
