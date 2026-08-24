# Bitwarden Secrets Manager (GitHub Actions)

This project uses Bitwarden Secrets Manager as the source of truth for long-lived automation secrets used by GitHub
Actions. Workflows fetch secrets at runtime through `bitwarden/sm-action`; the local Windows machine can read/update
the same project directly through the `bws` CLI (see `AGENTS.md` for the local-development flow).

Do not commit secret values. This file documents secret IDs, environment variable names, machine accounts, and the
repository variable mapping only.

## Bitwarden project

All GitHub Actions secrets for this repo live in the same Bitwarden Secrets Manager project used for local dev:

- Project: `Queenzone Development`
- Project ID: `1c16fd2d-4bfb-4eb7-8357-b49400233490`

## Configuration summary

| Context                | Machine account                       | Token location                    | Env var used by tool                     | Purpose                                                                      |
| ----------------------- | -------------------------------------- | ---------------------------------- | ------------------------------------------ | ------------------------------------------------------------------------------ |
| GitHub Actions          | `github-actions-queenzone` (read-only) | GitHub Actions secret              | `BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN` | Lets workflows fetch Bitwarden secrets through `bitwarden/sm-action@v3.0.1`. |
| Local Windows machine   | `windows-codex`                        | Windows User environment variable  | `BWS_ACCESS_TOKEN`                       | Lets this computer read/update Bitwarden Secrets Manager with `bws.exe`.     |

The two token environment variable names are intentionally different because they are consumed by different tools:

- `BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN`: passed to the GitHub Action's `access_token` input.
- `BWS_ACCESS_TOKEN`: read directly by Bitwarden's local Secrets Manager CLI.

## GitHub bootstrap secret

Keep exactly one GitHub Actions secret for Bitwarden access:

- `BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN`: access token for the `github-actions-queenzone` machine account, scoped
  read-only to the `Queenzone Development` project.

GitHub's built-in `GITHUB_TOKEN` remains GitHub-native because it is minted for each workflow run.

Every workflow that needs private settings follows this shape:

```yaml
- name: Fetch Bitwarden deploy secrets
  id: bitwarden-secrets
  uses: bitwarden/sm-action@v3.0.1
  with:
    access_token: ${{ secrets.BITWARDEN_SECRETS_MANAGER_ACCESS_TOKEN }}
    secrets: ${{ vars.BITWARDEN_APP_SERVICE_DEPLOY_SECRETS }}
```

and downstream steps read the outputs by name, e.g. `${{ steps.bitwarden-secrets.outputs.AZURE_WEBAPP_PUBLISH_PROFILE }}`.

The `vars.BITWARDEN_*_SECRETS` value is a repository variable containing mappings from Bitwarden secret IDs to
environment variable names. Those mappings are listed below.

## Bitwarden secret mappings

The Bitwarden action maps secret UUIDs to the environment variable names that the existing workflow steps already
consume:

```yaml
00000000-0000-0000-0000-000000000000 > ENVIRONMENT_VARIABLE_NAME
```

Store these mapping blocks as GitHub repository variables. The left-hand side must be the Bitwarden secret ID, not
the secret name.

### `BITWARDEN_APP_SERVICE_DEPLOY_SECRETS`

Used by `.github/workflows/deploy.yml` (`migrate` and `deploy` jobs), `.github/workflows/ci.yml`
(`ef-migrations` job — same-repo PRs touching migration paths only), and
`.github/workflows/nightly-legacy-checks.yml` (`sync-legacy-db` and `legacy-read-probes` jobs):

```yaml
743274c8-1837-4abd-b223-b4980080709f > AZURE_WEBAPP_PUBLISH_PROFILE
d631aa7c-4e7e-4d2d-b3ea-b494002d1b83 > QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING
51484e1f-4393-41e9-8435-b49a00381ec2 > QUEENZONE_SQL_EXPRESS_PROBE_PASSWORD
b6a94e02-3243-411f-8e32-b4af00ce2522 > MOBILE_AUTH_SIGNING_KEY
```

`QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` maps to the same underlying Bitwarden secret as the
`ConnectionStrings__QueenZoneLegacy` value used for local dev and Azure App Service settings (see `AGENTS.md`).
Updating that Bitwarden secret updates both; it does **not** update the live App Service runtime connection string,
which is configured separately in Azure App Service settings.

`QUEENZONE_SQL_EXPRESS_PROBE_PASSWORD` is the password for the `queenzone_probe` SQL login created by
`scripts/Enable-SqlExpressRemoteAccess.ps1` on the Windows self-hosted runner. It authenticates
`legacy-read-probes` (running on the macOS runner) against the SQL Express mirror over the LAN — see
`docs/architecture/testing-policy.md` ("Data Integration Tests") and the comment block at the top of
`nightly-legacy-checks.yml`.

`MOBILE_AUTH_SIGNING_KEY` maps to the `MobileAuth__SigningKey` secret. The deploy workflow reconciles it
to the same-named App Service setting before every web deployment, including rotations, and fails before
deployment if the mapped value is missing or shorter than 32 characters.

## Android FCM push credential

Android push uses Firebase project `queenzone-mobile` and its registered Android app
`org.queenzone.mobile`. The committed `src/QueenZone.Mobile/google-services.json` contains Firebase project/app
identifiers used by the Android client; Firebase documents this file and its API key as non-secret client
configuration. It must not be confused with the private service-account JSON below.

The backend FCM HTTP v1 sender is the dedicated Google Cloud service account
`queenzone-fcm-sender@queenzone-mobile.iam.gserviceaccount.com`. It has only the **Firebase Cloud Messaging API
Admin** role. Its private key is stored under these matching Bitwarden and Azure App Service names:

- `PushNotifications__Fcm__ProjectId`: `queenzone-mobile` (identifier, not a credential).
- `PushNotifications__Fcm__ServiceAccountJson`: the complete private service-account JSON. Never print, commit,
  or paste this value into an issue or pull request.

To rotate the credential, create a new JSON key on the same dedicated service account. Update Bitwarden first,
then update both matching App Service settings and restart/verify the site. Confirm an authenticated FCM HTTP v1
send succeeds before permanently deleting the previous key in Google Cloud. Verify values only by exact in-memory
comparison or value length; never emit the JSON or its private key.

## Rotation and break-glass (App Service settings)

[ADR 0008](decisions/0008-app-service-settings-ownership.md) keeps App Service application settings outside
OpenTofu (Option A of [#618](https://github.com/richardorchard/QueenZone.Modern/issues/618)), so rotation and
break-glass for those settings stay entirely inside the Bitwarden/Azure/GitHub workflow described here — there is
no OpenTofu apply/plan step to run or wait on.

**Normal rotation** (Bitwarden reachable): follow AGENTS.md's Bitwarden section — update the value in Bitwarden
first, then update the same key in Azure App Service configuration (portal or `az webapp config appsettings set`),
then restart/verify. Bitwarden and the live App Service value are two separate stores; updating one never updates
the other automatically, except for `MobileAuth__SigningKey`: the next web deployment reconciles that key from
Bitwarden before deploying. Apply it to App Service directly when the rotation must take effect immediately.
Verify by setting name and value length only, never by printing the value.

**Break-glass** (Bitwarden Secrets Manager unreachable, or the `BWS_ACCESS_TOKEN` / `github-actions-queenzone`
machine account is unavailable):

1. An operator with Azure portal/CLI access to `Queenzone-RG` may set an App Service setting directly
   (`az webapp config appsettings set --name queenzone-dev --resource-group Queenzone-RG --settings
   KEY=VALUE`), bypassing Bitwarden for that one change. This is the same access path
   `.github/workflows/deploy.yml`'s `configure-app-settings` job already uses via OIDC — no new credential to
   provision.
2. Restart the App Service (or let the next deploy's warmup do so) and confirm the app comes up healthy.
3. As soon as Bitwarden access is restored, write the same value back into the `Queenzone Development` project so
   Bitwarden remains the source of truth for automation/local recovery — a break-glass change that never gets
   reconciled back into Bitwarden will silently drift on the next rotation.
4. Record what changed (setting name and reason, never the value) in the PR or issue tracking the incident that
   forced the break-glass path, so `docs/architecture/opentofu-inventory.md`'s setting-name inventory can be
   updated if a name was added or removed. `scripts/Test-AppServiceSettingNames.ps1`
   (`.github/workflows/app-service-setting-names-check.yml`) only checks that required names exist — it does not
   detect drifted values, so a break-glass value change is otherwise invisible to automation.

There is no multi-approver process for break-glass today — QueenZone has a single operator with both Bitwarden and
Azure access. If that changes, revisit this section.

## Migration notes

GitHub Actions secrets cannot be read back in plaintext after they have been saved. GitHub can list secret names and
overwrite secret values, but it cannot export their current values.

For this migration:

- `AZURE_WEBAPP_PUBLISH_PROFILE` was regenerated fresh from Azure (`az webapp deployment list-publishing-profiles
  --name queenzone-dev --resource-group Queenzone-RG --xml`) and written into Bitwarden, since the old GitHub secret
  value could not be recovered.
- `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` reused the existing `ConnectionStrings__QueenZoneLegacy` Bitwarden
  secret, which already held the same connection string for local dev.

The old raw GitHub Actions secrets (`AZURE_WEBAPP_PUBLISH_PROFILE`, `QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING`)
were removed once the deploy workflow (`deploy-app-service.yml` at the time, since renamed to `deploy.yml`) was
verified end-to-end via a real deploy run using the Bitwarden-sourced values. The stale
`CONNECTIONSTRINGS__QUEENZONELEGACY` secret (unused by any workflow) was removed at the same time.
