# ADR 0013: Static Web App for mobile test distribution

## Status

Accepted

## Context

GitHub Actions produces an Android APK, but workflow artifacts require a GitHub login, arrive inside a ZIP, and expire after one day. The maintainer needs a stable link that can be opened directly on an Android phone.

An Azure Static Web App named `queenzone-dev` already exists. Issue #809 originally proposed a dedicated Storage account with static website hosting, but creating a second hosting resource would duplicate the existing capability.

## Decision

Use the existing `queenzone-dev` Azure Static Web App for public test-build distribution at `https://dev.queenzone.org`.

A dedicated GitHub Actions workflow:

- builds a standalone Android release APK against the staging API;
- signs every build with one stable test-only key held in Bitwarden Secrets Manager and fetched by GitHub Actions at runtime;
- assigns the GitHub Actions run number as the Android version code so a newer download is accepted as an upgrade;
- overwrites `queenzone-latest.apk` and publishes build metadata in Western Australian time; and
- deploys through the existing GitHub OIDC identity, scoped separately to this Static Web App.

The site is public by URL. It carries `noindex` directives but has no authentication. iOS is excluded until CI produces a signed, device-installable build.

## Consequences

Android can install later builds as upgrades because their package identifier and signing key remain stable. Losing or rotating the test key requires uninstalling the existing test app before installing the next build.

The distribution page and APK are intentionally public. They must contain no secrets or production-only credentials. The Static Web App remains isolated from production UGC and media storage.
