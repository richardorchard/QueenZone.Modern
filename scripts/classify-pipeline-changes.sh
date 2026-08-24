#!/usr/bin/env bash
# Reads changed paths on stdin (one per line) and writes GitHub Actions
# outputs `code=`, `migrations=`, `mobile=`, and `mobile_api_contracts=` to stdout.
#
# code=true means the .NET web app / test / deploy path should run.
# code=false when every path is docs / infra / design / root markdown /
# LICENSE / THIRD-PARTY-NOTICES / .github, except that a change to
# .github/workflows/ci.yml is always code (the suite must still run to
# validate itself), or mobile-only (src/QueenZone.Mobile/). Empty stdin
# fails closed as code=true (and mobile_api_contracts=true).
#
# mobile=true when any path is under src/QueenZone.Mobile/, or when the
# mobile coverage gate / floors change (those run inside mobile-js).
# A mobile-only change does not set code=true: the website binary is
# unchanged, so web tests and App Service deploy should not run.
# Mixed mobile + web still sets both flags.
# Native Android/iOS jobs stay on mobile=true only.
#
# mobile_api_contracts=true is independent of mobile=true. It runs the
# consumer-contract suite (Testing host + real mobile client parsers)
# without forcing native compile jobs. Empty stdin fails closed as true.
#
# migrations=true when any path is under the EF migration / model set
# used by ci.yml's ef-migrations job.
set -euo pipefail

skip_re='^(docs/|infra/|design/|[^/]*\.md$|LICENSE$|THIRD-PARTY-NOTICES\.md$|\.github/)'
mobile_re='^src/QueenZone\.Mobile(/|$)'
mobile_coverage_re='^scripts/(Test-MobileCoverageGate\.mjs|mobile-coverage-floors\.json)$'
migration_re='^(src/QueenZone\.Data/Migrations/|src/QueenZone\.Data/QueenZoneDbContext\.cs|src/QueenZone\.Data/QueenZoneDbContextFactory\.cs|src/QueenZone\.Data/Entities/)'
# Keep this list explicit so an unclassified contract path is a classifier
# bug, not a silent skip. Fail closed: empty input sets the flag true.
mobile_api_contracts_re='^(src/QueenZone\.Web/Api/|src/QueenZone\.Web/Infrastructure/MobileApiContractHost\.cs|src/QueenZone\.Web/Infrastructure/QueenZoneWebServiceCollectionExtensions\.cs|docs/architecture/json-api-v1\.md|docs/decisions/0010-versioned-json-api-conventions\.md|src/QueenZone\.Mobile/src/api/|src/QueenZone\.Mobile/src/config/|src/QueenZone\.Mobile/src/session/|src/QueenZone\.Mobile/src/screens/photos/photoGalleryMeta\.ts|src/QueenZone\.Mobile/apiEnvironments\.cjs|src/QueenZone\.Mobile/contracts/|src/QueenZone\.Mobile/scripts/run-api-contracts|scripts/run-mobile-api-contracts\.sh|scripts/classify-pipeline-changes\.sh|\.github/workflows/ci\.yml|tests/QueenZone\.Web\.Tests/MobileApiContract)'

classify() {
  local code=false
  local migrations=false
  local mobile=false
  local mobile_api_contracts=false
  local saw_any=false
  local path

  while IFS= read -r path || [ -n "${path}" ]; do
    [ -z "${path}" ] && continue
    saw_any=true

    if printf '%s\n' "${path}" | grep -qE "${mobile_re}|${mobile_coverage_re}"; then
      mobile=true
    fi

    if [ "${path}" = ".github/workflows/ci.yml" ]; then
      code=true
    elif printf '%s\n' "${path}" | grep -qE "${mobile_re}"; then
      :
    elif ! printf '%s\n' "${path}" | grep -qE "${skip_re}"; then
      code=true
    fi

    if printf '%s\n' "${path}" | grep -qE "${migration_re}"; then
      migrations=true
    fi

    if printf '%s\n' "${path}" | grep -qE "${mobile_api_contracts_re}"; then
      mobile_api_contracts=true
    fi
  done

  if [ "${saw_any}" = false ]; then
    echo "No changed paths on stdin — failing closed as code=true and mobile_api_contracts=true." >&2
    code=true
    mobile_api_contracts=true
  fi

  if [ "${code}" = true ]; then
    echo "Web/app/test/CI paths changed — full .NET suite (and deploy, after merge)." >&2
  elif [ "${mobile}" = true ]; then
    echo "Mobile-only change — skipping web build, tests, and deploy." >&2
  else
    echo "Docs/infra/design/workflow-only change — skipping build, tests, and deploy." >&2
  fi

  if [ "${mobile}" = true ]; then
    echo "Mobile client paths changed — will run mobile JS checks." >&2
  fi

  if [ "${mobile_api_contracts}" = true ]; then
    echo "Mobile API contract paths changed — will run consumer-contract suite (not native compiles)." >&2
  fi

  if [ "${migrations}" = true ]; then
    echo "EF migration-related paths changed — will run Azure SQL migration gate." >&2
  fi

  echo "code=${code}"
  echo "migrations=${migrations}"
  echo "mobile=${mobile}"
  echo "mobile_api_contracts=${mobile_api_contracts}"
}

assert_classify() {
  local name="$1"
  local expected="$2"
  shift 2
  local got
  got=$(printf '%s\n' "$@" | classify | grep -E '^(code|migrations|mobile|mobile_api_contracts)=')
  if [ "${got}" != "${expected}" ]; then
    echo "FAIL ${name}" >&2
    echo " expected:" >&2
    echo "${expected}" >&2
    echo " got:" >&2
    echo "${got}" >&2
    return 1
  fi
  echo "PASS ${name}" >&2
}

if [ "${1:-}" = "--self-test" ]; then
  fail=0
  nl=$'\n'

  assert_classify docs-only \
    "code=false${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=false" \
    "docs/architecture/testing-policy.md" \
    || fail=1

  assert_classify json-api-docs \
    "code=false${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=true" \
    "docs/architecture/json-api-v1.md" \
    || fail=1

  assert_classify web-cs \
    "code=true${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=false" \
    "src/QueenZone.Web/Program.cs" \
    || fail=1

  assert_classify api-only \
    "code=true${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=true" \
    "src/QueenZone.Web/Api/Content/ContentApiModels.cs" \
    || fail=1

  assert_classify contract-host-registration \
    "code=true${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=true" \
    "src/QueenZone.Web/Infrastructure/QueenZoneWebServiceCollectionExtensions.cs" \
    || fail=1

  assert_classify mobile-only \
    "code=false${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=false" \
    "src/QueenZone.Mobile/App.tsx" \
    "src/QueenZone.Mobile/package.json" \
    || fail=1

  assert_classify mobile-api-client \
    "code=false${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=true" \
    "src/QueenZone.Mobile/src/api/client.ts" \
    || fail=1

  assert_classify mobile-plus-docs \
    "code=false${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=false" \
    "src/QueenZone.Mobile/src/navigation/RootNavigator.tsx" \
    "AGENTS.md" \
    || fail=1

  assert_classify mixed-web-and-mobile \
    "code=true${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=false" \
    "src/QueenZone.Mobile/App.tsx" \
    "src/QueenZone.Web/Program.cs" \
    || fail=1

  assert_classify mixed-api-and-mobile \
    "code=true${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=true" \
    "src/QueenZone.Mobile/src/api/forum.ts" \
    "src/QueenZone.Web/Api/Forum/ForumApiEndpoints.cs" \
    || fail=1

  assert_classify ci-yml \
    "code=true${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=true" \
    ".github/workflows/ci.yml" \
    || fail=1

  assert_classify deploy-yml-only \
    "code=false${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=false" \
    ".github/workflows/deploy.yml" \
    || fail=1

  assert_classify mobile-coverage-gate \
    "code=true${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=false" \
    "scripts/Test-MobileCoverageGate.mjs" \
    || fail=1

  assert_classify mobile-coverage-floors \
    "code=true${nl}migrations=false${nl}mobile=true${nl}mobile_api_contracts=false" \
    "scripts/mobile-coverage-floors.json" \
    || fail=1

  assert_classify migrations \
    "code=true${nl}migrations=true${nl}mobile=false${nl}mobile_api_contracts=false" \
    "src/QueenZone.Data/Migrations/20260821_Example.cs" \
    || fail=1

  assert_classify empty \
    "code=true${nl}migrations=false${nl}mobile=false${nl}mobile_api_contracts=true" \
    "" \
    || fail=1

  if [ "${fail}" -ne 0 ]; then
    echo "classify-pipeline-changes self-test failed." >&2
    exit 1
  fi
  echo "classify-pipeline-changes self-test passed." >&2
  exit 0
fi

classify
