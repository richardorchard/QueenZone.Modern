# Azure mobile builds

Dedicated low-cost storage for public pre-release mobile binaries. The
`builds` container allows anonymous reads of known blob URLs but does not allow
container listing. It must not hold production UGC, media, secrets, or release
signing keys.

GitHub Actions authenticates through the existing `deploy` OIDC identity. Its
`Storage Blob Data Contributor` role is scoped to this account only. OpenTofu
manages no blob objects.

Sideload APK publish (`publish-mobile-test-build.yml`) was retired in #1306
and no longer overwrites `queenzone-latest.apk`. This account remains until a
separate Azure cleanup; do not treat it as the tester distribution path.
