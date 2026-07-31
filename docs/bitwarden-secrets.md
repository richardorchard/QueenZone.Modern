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

Used by `.github/workflows/deploy-app-service.yml` (`migrate` and `deploy` jobs):

```yaml
743274c8-1837-4abd-b223-b4980080709f > AZURE_WEBAPP_PUBLISH_PROFILE
d631aa7c-4e7e-4d2d-b3ea-b494002d1b83 > QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING
```

`QUEENZONE_LEGACY_MIGRATION_CONNECTION_STRING` maps to the same underlying Bitwarden secret as the
`ConnectionStrings__QueenZoneLegacy` value used for local dev and Azure App Service settings (see `AGENTS.md`).
Updating that Bitwarden secret updates both; it does **not** update the live App Service runtime connection string,
which is configured separately in Azure App Service settings.

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
were removed once `deploy-app-service.yml` was verified end-to-end via a real deploy run using the Bitwarden-sourced
values. The stale `CONNECTIONSTRINGS__QUEENZONELEGACY` secret (unused by any workflow) was removed at the same time.
