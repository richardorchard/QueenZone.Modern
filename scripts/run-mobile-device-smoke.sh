#!/usr/bin/env bash
# Boot the Testing contract host and run the Maestro device smoke suite
# against a Debug build baked at the loopback Testing origin (#872 Option A).
#
# Never points Testing at a real database, blob store, live site, or OAuth.
# Usage (repo root):
#   ./scripts/run-mobile-device-smoke.sh --platform android
#   ./scripts/run-mobile-device-smoke.sh --platform ios
#   ./scripts/run-mobile-device-smoke.sh --platform android --skip-build --apk path/to/app-debug.apk
#   ./scripts/run-mobile-device-smoke.sh --platform android --prove-failure
#   ./scripts/run-mobile-device-smoke.sh --platform android --suite journeys
#
# Maestro flows are not retried. A single emulator/simulator boot failure is
# the runner's problem (android-emulator-runner / simctl), not a test retry.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"

platform=""
skip_build=false
skip_host=false
no_build_host=false
prove_failure=false
suite="smoke"
apk=""
app=""
port="${SMOKE_PORT:-5098}"
results_dir="${SMOKE_RESULTS_DIR:-$root/src/QueenZone.Mobile/maestro-results}"
fixture="${QUEENZONE_MOBILE_CONTRACT_FIXTURE:-$root/src/QueenZone.Mobile/contracts/host.json}"
host_log="${SMOKE_HOST_LOG:-$results_dir/contract-host.log}"

usage() {
  sed -n '2,16p' "$0" | sed 's/^# \{0,1\}//'
}

while [ "$#" -gt 0 ]; do
  case "$1" in
    --platform)
      platform="${2:-}"
      shift 2
      ;;
    --skip-build)
      skip_build=true
      shift
      ;;
    --skip-host)
      skip_host=true
      shift
      ;;
    --no-build)
      no_build_host=true
      shift
      ;;
    --prove-failure)
      prove_failure=true
      shift
      ;;
    --suite)
      suite="${2:-}"
      shift 2
      ;;
    --apk)
      apk="${2:-}"
      shift 2
      ;;
    --app)
      app="${2:-}"
      shift 2
      ;;
    --port)
      port="${2:-}"
      shift 2
      ;;
    --results-dir)
      results_dir="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if [ "$platform" != "android" ] && [ "$platform" != "ios" ]; then
  echo "--platform android|ios is required." >&2
  exit 2
fi

if [ "$suite" != "smoke" ] && [ "$suite" != "journeys" ]; then
  echo "--suite smoke|journeys is required (default smoke)." >&2
  exit 2
fi

if [ "$prove_failure" = true ] && [ "$suite" != "smoke" ]; then
  echo "--prove-failure is only valid with --suite smoke." >&2
  exit 2
fi

if [ "$platform" = "ios" ] && [ "$(uname -s)" != "Darwin" ]; then
  echo "iOS Simulator smoke requires macOS. Document CI-only verification on Linux agents." >&2
  exit 2
fi

unset ConnectionStrings__QueenZoneLegacy || true
unset ConnectionStrings__BlobStorage || true
unset ConnectionStrings__SqlServerTest || true

mkdir -p "$results_dir"
export QUEENZONE_MOBILE_CONTRACT_FIXTURE="$fixture"

host_pid=""
cleanup() {
  if [ -n "${host_pid}" ] && kill -0 "$host_pid" 2>/dev/null; then
    kill "$host_pid" 2>/dev/null || true
    wait "$host_pid" 2>/dev/null || true
  fi
}
trap cleanup EXIT

start_host() {
  export ASPNETCORE_ENVIRONMENT=Testing
  export QUEENZONE_MOBILE_CONTRACT_HOST=1
  export ASPNETCORE_URLS="http://127.0.0.1:${port}"

  rm -f "$fixture" "${fixture}.tmp"

  if [ "$no_build_host" != true ]; then
    dotnet build src/QueenZone.Web/QueenZone.Web.csproj --configuration Release
  fi

  echo "Starting Testing contract host on ${ASPNETCORE_URLS} (log: $host_log)..."
  dotnet run \
    --project src/QueenZone.Web/QueenZone.Web.csproj \
    --configuration Release \
    --no-build \
    --no-launch-profile \
    >"$host_log" 2>&1 &
  host_pid=$!

  for _ in $(seq 1 90); do
    if [ -f "$fixture" ]; then
      break
    fi
    if ! kill -0 "$host_pid" 2>/dev/null; then
      echo "Contract host exited before writing the fixture." >&2
      cat "$host_log" >&2 || true
      exit 1
    fi
    sleep 1
  done

  if [ ! -f "$fixture" ]; then
    echo "Timed out waiting for $fixture" >&2
    cat "$host_log" >&2 || true
    exit 1
  fi

  if ! curl -sf "http://127.0.0.1:${port}/health" >/dev/null; then
    echo "Health check failed for http://127.0.0.1:${port}/health" >&2
    cat "$host_log" >&2 || true
    exit 1
  fi

  echo "Contract host ready at http://127.0.0.1:${port}"
}

export_smoke_auth_url() {
  if [ ! -f "$fixture" ]; then
    echo "Contract fixture $fixture is missing; cannot build SMOKE_AUTH_URL." >&2
    exit 1
  fi

  SMOKE_AUTH_URL="$(
    node -e '
      const fs = require("fs");
      const fixture = JSON.parse(fs.readFileSync(process.env.QUEENZONE_MOBILE_CONTRACT_FIXTURE, "utf8"));
      if (fixture.environment !== "Testing") {
        throw new Error("Smoke auth refuses a non-Testing fixture environment: " + fixture.environment);
      }
      const token = fixture.member && fixture.member.accessToken;
      if (!token) {
        throw new Error("Contract fixture is missing member.accessToken.");
      }
      process.stdout.write("queenzone://smoke-auth?accessToken=" + encodeURIComponent(token));
    '
  )"
  export SMOKE_AUTH_URL
  echo "SMOKE_AUTH_URL is set (length ${#SMOKE_AUTH_URL}; token not printed)."
}

export_journey_env() {
  if [ ! -f "$fixture" ]; then
    echo "Contract fixture $fixture is missing; cannot export journey IDs." >&2
    exit 1
  fi

  ATTACH_TOPIC_ID="$(
    node -e '
      const fs = require("fs");
      const fixture = JSON.parse(fs.readFileSync(process.env.QUEENZONE_MOBILE_CONTRACT_FIXTURE, "utf8"));
      if (fixture.environment !== "Testing") {
        throw new Error("Journeys refuse a non-Testing fixture environment: " + fixture.environment);
      }
      const id = fixture.attachTopicId;
      if (!Number.isInteger(id) || id <= 0) {
        throw new Error("Contract fixture is missing attachTopicId.");
      }
      process.stdout.write(String(id));
    '
  )"
  export ATTACH_TOPIC_ID
  echo "ATTACH_TOPIC_ID is set."
}

push_attach_fixture() {
  local src="$root/src/QueenZone.Mobile/maestro/fixtures/attach.txt"
  if [ ! -f "$src" ]; then
    echo "Missing attach fixture at $src" >&2
    exit 1
  fi

  if [ "$platform" = "android" ]; then
    adb shell mkdir -p /sdcard/Download
    adb push "$src" /sdcard/Download/attach.txt >/dev/null
    adb shell mkdir -p /sdcard/Android/data/org.queenzone.mobile/files
    adb push "$src" /sdcard/Android/data/org.queenzone.mobile/files/attach.txt >/dev/null
    SMOKE_ATTACH_URL="$(
      node -e 'process.stdout.write("queenzone://smoke-attach?uri=" + encodeURIComponent("file:///sdcard/Download/attach.txt") + "&name=attach.txt&type=text/plain")'
    )"
  else
    local data
    data="$(xcrun simctl get_app_container booted org.queenzone.mobile data)"
    if [ -z "$data" ] || [ ! -d "$data" ]; then
      echo "Could not resolve the iOS Simulator data container for org.queenzone.mobile." >&2
      exit 1
    fi
    mkdir -p "$data/Documents"
    cp "$src" "$data/Documents/attach.txt"
    SMOKE_ATTACH_URL="$(
      node -e 'process.stdout.write("queenzone://smoke-attach?uri=" + encodeURIComponent(process.argv[1]) + "&name=attach.txt&type=text/plain")' \
        "file://${data}/Documents/attach.txt"
    )"
  fi
  export SMOKE_ATTACH_URL
  echo "SMOKE_ATTACH_URL is set (length ${#SMOKE_ATTACH_URL}; path not printed)."
}

build_android() {
  (
    cd src/QueenZone.Mobile
    export EXPO_PUBLIC_APP_ENV=development
    export EXPO_PUBLIC_API_BASE_URL="http://10.0.2.2:${port}"
    export SENTRY_DISABLE_AUTO_UPLOAD=true
    echo "Baking Android smoke APK for ${EXPO_PUBLIC_API_BASE_URL}"
    npx expo prebuild --platform android
    (
      cd android
      ./gradlew assembleDebug
    )
  )
  apk="$root/src/QueenZone.Mobile/android/app/build/outputs/apk/debug/app-debug.apk"
}

build_ios() {
  (
    cd src/QueenZone.Mobile
    export EXPO_PUBLIC_APP_ENV=development
    export EXPO_PUBLIC_API_BASE_URL="http://127.0.0.1:${port}"
    export SENTRY_DISABLE_AUTO_UPLOAD=true
    echo "Baking iOS Simulator smoke app for ${EXPO_PUBLIC_API_BASE_URL}"
    npx expo prebuild --platform ios --clean
    cd ios
    workspace="$(ls -d *.xcworkspace | head -n 1)"
    scheme="$(basename "$workspace" .xcworkspace)"
    xcodebuild \
      -workspace "$workspace" \
      -scheme "$scheme" \
      -sdk iphonesimulator \
      -configuration Debug \
      -derivedDataPath build \
      CODE_SIGNING_ALLOWED=NO \
      build
  )
  app="$(ls -d "$root"/src/QueenZone.Mobile/ios/build/Build/Products/Debug-iphonesimulator/*.app | head -n 1)"
}

if [ "$skip_host" != true ]; then
  start_host
else
  echo "Skipping host start; expecting an already-running Testing contract host."
  if ! curl -sf "http://127.0.0.1:${port}/health" >/dev/null; then
    echo "Health check failed for http://127.0.0.1:${port}/health" >&2
    exit 1
  fi
fi

export_smoke_auth_url
if [ "$suite" = "journeys" ]; then
  export_journey_env
fi

if [ "$skip_build" != true ]; then
  if [ "$platform" = "android" ]; then
    build_android
  else
    build_ios
  fi
fi

if [ "$platform" = "android" ] && [ -z "$apk" ]; then
  apk="$root/src/QueenZone.Mobile/android/app/build/outputs/apk/debug/app-debug.apk"
fi
if [ "$platform" = "ios" ] && [ -z "$app" ]; then
  app="$(ls -d "$root"/src/QueenZone.Mobile/ios/build/Build/Products/Debug-iphonesimulator/*.app 2>/dev/null | head -n 1 || true)"
fi

if [ "$platform" = "android" ]; then
  if [ -z "$apk" ] || [ ! -f "$apk" ]; then
    echo "Android smoke APK not found at '${apk:-<empty>}'." >&2
    exit 1
  fi
  if ! command -v adb >/dev/null; then
    echo "adb is required to install the Android smoke APK." >&2
    exit 1
  fi
  echo "Installing $apk"
  adb wait-for-device
  adb install -r "$apk"
else
  if [ -z "$app" ] || [ ! -d "$app" ]; then
    echo "iOS Simulator .app not found at '${app:-<empty>}'." >&2
    exit 1
  fi
  echo "Installing $app"
  xcrun simctl install booted "$app"
fi

if [ "$suite" = "journeys" ]; then
  push_attach_fixture
fi

if ! command -v maestro >/dev/null; then
  echo "Maestro is not on PATH. Install with: curl -Ls \"https://get.maestro.mobile.dev\" | bash" >&2
  exit 1
fi

flow="src/QueenZone.Mobile/maestro/smoke.yaml"
if [ "$prove_failure" = true ]; then
  flow="src/QueenZone.Mobile/maestro/prove-failure.yaml"
  echo "Running forced-assertion flow to prove failure artifacts."
elif [ "$suite" = "journeys" ]; then
  flow="src/QueenZone.Mobile/maestro/journeys.yaml"
  echo "Running on-demand Maestro journeys (#1071)."
fi

echo "Running Maestro ($flow). Flows are not retried."
set +e
maestro_args=(
  test "$flow"
  --format junit
  --output "$results_dir/junit.xml"
  --debug-output "$results_dir/debug"
  --flatten-debug-output
  -e "SMOKE_AUTH_URL=${SMOKE_AUTH_URL}"
)
if [ "$suite" = "journeys" ]; then
  maestro_args+=(
    -e "SMOKE_ATTACH_URL=${SMOKE_ATTACH_URL}"
    -e "ATTACH_TOPIC_ID=${ATTACH_TOPIC_ID}"
  )
fi
maestro "${maestro_args[@]}"
maestro_status=$?
set -e

if [ "$maestro_status" -ne 0 ]; then
  echo "Maestro failed with status $maestro_status" >&2
  if [ "$platform" = "android" ] && command -v adb >/dev/null; then
    adb logcat -d > "$results_dir/logcat.txt" || true
  fi
  if [ "$platform" = "ios" ]; then
    xcrun simctl spawn booted log show --last 5m --style compact \
      > "$results_dir/simulator.log" 2>/dev/null || true
  fi
  cp "$host_log" "$results_dir/contract-host.log" 2>/dev/null || true
fi

exit "$maestro_status"
