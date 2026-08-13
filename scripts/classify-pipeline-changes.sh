#!/usr/bin/env bash
# Reads changed paths on stdin (one per line) and writes GitHub Actions
# outputs `code=` and `migrations=` to stdout.
#
# code=false when every path is docs / infra / design / root markdown /
# LICENSE / THIRD-PARTY-NOTICES / .github, except that a change to
# .github/workflows/ci.yml is always code (the suite must still run to
# validate itself). Empty stdin fails closed as code=true.
#
# migrations=true when any path is under the EF migration / model set
# used by ci.yml's ef-migrations job.
set -euo pipefail

skip_re='^(docs/|infra/|design/|[^/]*\.md$|LICENSE$|THIRD-PARTY-NOTICES\.md$|\.github/)'
migration_re='^(src/QueenZone\.Data/Migrations/|src/QueenZone\.Data/QueenZoneDbContext\.cs|src/QueenZone\.Data/QueenZoneDbContextFactory\.cs|src/QueenZone\.Data/Entities/)'

code=false
migrations=false
saw_any=false

while IFS= read -r path || [ -n "${path}" ]; do
  [ -z "${path}" ] && continue
  saw_any=true

  if [ "${path}" = ".github/workflows/ci.yml" ]; then
    code=true
  elif ! printf '%s\n' "${path}" | grep -qE "${skip_re}"; then
    code=true
  fi

  if printf '%s\n' "${path}" | grep -qE "${migration_re}"; then
    migrations=true
  fi
done

if [ "${saw_any}" = false ]; then
  echo "No changed paths on stdin — failing closed as code=true." >&2
  code=true
fi

if [ "${code}" = true ]; then
  echo "App/test/CI paths changed — full suite (and deploy, after merge)." >&2
else
  echo "Docs/infra/design/workflow-only change — skipping build, tests, and deploy." >&2
fi

if [ "${migrations}" = true ]; then
  echo "EF migration-related paths changed — will run Azure SQL migration gate." >&2
fi

echo "code=${code}"
echo "migrations=${migrations}"
