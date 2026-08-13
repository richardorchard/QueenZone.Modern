#!/usr/bin/env bash
# Public-domain smoke used by deploy.yml (and locally via bash).
# Default: wait for /warmup, then require 200 on the content routes.
# --warmup-only: only the /warmup gate (used in the deploy job so recycle
# overlaps the zip push instead of waiting for a second runner).
set -euo pipefail

BASE_URL="https://www.queenzone.org"
WARMUP_ONLY=0
MAX_ATTEMPTS=32
SLEEP_SECONDS=15

while [ "$#" -gt 0 ]; do
  case "$1" in
    --warmup-only) WARMUP_ONLY=1 ;;
    --base-url) BASE_URL="${2:?}"; shift ;;
    --max-attempts) MAX_ATTEMPTS="${2:?}"; shift ;;
    --sleep-seconds) SLEEP_SECONDS="${2:?}"; shift ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 2
      ;;
  esac
  shift
done

WARMUP_PATH="/warmup"
PATHS=(
  "/health"
  "/"
  "/news"
  "/forum"
  "/forum/1/queen-serious-discussion"
  "/forum/topic/455095/forum-guidelines"
  "/articles"
  "/biography"
  "/photography"
  "/search"
)

check_path() {
  local path="$1"
  local url="${BASE_URL}${path}"
  local status body_file
  body_file="$(mktemp)"
  status=$(curl -s -o "$body_file" -w "%{http_code}" --max-time 30 -L "$url") || status="${status:-000}"
  if [ "$status" != "200" ]; then
    echo "  ✗ $path → HTTP $status"
    rm -f "$body_file"
    return 1
  fi
  if [ "$path" = "/health" ] || [ "$path" = "$WARMUP_PATH" ]; then
    if ! grep -q '"status":"ok"' "$body_file"; then
      echo "  ✗ $path → 200 but body missing \"status\":\"ok\""
      rm -f "$body_file"
      return 1
    fi
  fi
  echo "  ✓ $path → HTTP $status"
  rm -f "$body_file"
  return 0
}

echo "Waiting for warmup on ${BASE_URL}${WARMUP_PATH} (up to ~$(( MAX_ATTEMPTS * SLEEP_SECONDS / 60 )) minutes)."
echo "/health is not sufficient readiness — App Service can answer liveness while pages still 500."

for attempt in $(seq 1 "$MAX_ATTEMPTS"); do
  echo "Warmup attempt ${attempt}/${MAX_ATTEMPTS}..."
  if check_path "$WARMUP_PATH"; then
    echo "Warmup passed on attempt ${attempt}."
    if [ "$WARMUP_ONLY" -eq 1 ]; then
      exit 0
    fi
    break
  fi

  if [ "$attempt" -eq "$MAX_ATTEMPTS" ]; then
    echo "::error::Warmup failed against ${BASE_URL}${WARMUP_PATH} after ${MAX_ATTEMPTS} attempts. Check App Service logs."
    exit 1
  fi

  echo "Warmup not ready yet. Retrying in ${SLEEP_SECONDS}s..."
  sleep "$SLEEP_SECONDS"
done

echo "Polling content routes on $BASE_URL after warmup."
for attempt in $(seq 1 "$MAX_ATTEMPTS"); do
  echo "Attempt ${attempt}/${MAX_ATTEMPTS}..."
  failed=0
  for path in "${PATHS[@]}"; do
    if ! check_path "$path"; then
      failed=1
    fi
  done

  if [ "$failed" -eq 0 ]; then
    echo "All post-deploy smoke checks passed on attempt ${attempt}."
    exit 0
  fi

  if [ "$attempt" -lt "$MAX_ATTEMPTS" ]; then
    echo "Site not ready (App Service recycle/warmup). Retrying in ${SLEEP_SECONDS}s..."
    sleep "$SLEEP_SECONDS"
  fi
done

echo "::error::Post-deploy smoke failed against $BASE_URL after ${MAX_ATTEMPTS} attempts. Check App Service logs."
exit 1
