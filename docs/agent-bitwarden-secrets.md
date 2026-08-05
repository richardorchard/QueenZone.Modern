# Bitwarden Secrets Manager for local agents

Shared local secret store for development agents on Richard’s machines. **This is not the Bitwarden password vault.**

| Tool | Product | Used for QueenZone agents? |
| --- | --- | --- |
| **`bws`** | Bitwarden **Secrets Manager** CLI | **Yes** — project secrets, machine access tokens |
| **`bw`** | Bitwarden **password manager** CLI | **No** for QueenZone App Service-style secrets (agents should not use `bw login` for this) |

## Shared project

| Field | Value |
| --- | --- |
| Secrets Manager project name | `Queenzone Development` |
| Project id | `1c16fd2d-4bfb-4eb7-8357-b49400233490` |
| Auth | User-scoped environment variable **`BWS_ACCESS_TOKEN`** (machine account access token) |
| Canonical secret keys | App Service names, e.g. `ConnectionStrings__QueenZoneLegacy`, `ConnectionStrings__BlobStorage`, `AzureAd__*`, `OPENROUTER_API_KEY` |

Do **not** commit the token, paste it into chat, or print secret **values**. Prefer reporting only key names and value **lengths** when verifying.

Bitwarden is a **local/recovery mirror**. Updating Bitwarden does **not** update Azure App Service or GitHub environment secrets.

## Machine accounts (platform-specific)

Create / use the machine account that matches the host OS. Each machine has its own access token stored as **user** environment variable `BWS_ACCESS_TOKEN` on that host.

| Host platform | Bitwarden machine account | Notes |
| --- | --- | --- |
| **Windows** (this workstation: user `me`, tools under `%USERPROFILE%\bin`) | `windows-codex` | Token: User env `BWS_ACCESS_TOKEN`. CLI: `%USERPROFILE%\bin\bws.exe` on PATH via User `Path` (`%USERPROFILE%\bin`). |
| **macOS** | `mac-codex` | Token: user env or shell profile (`~/.zprofile` / `~/.zshrc`). CLI on PATH (Homebrew or `~/bin`). |

Agents running on Windows must use the Windows machine token; agents on Mac must use the Mac machine token. Do not copy tokens between machines in git or chat.

### This Windows computer (current agent host)

Configured once for local agents (Grok, Codex, Claude, etc.):

1. **`BWS_ACCESS_TOKEN`** — User environment variable (not Machine, not Process-only). Length is typically ~90–100 characters; verify with presence/length only.
2. **`bws` CLI** — Installed at `%USERPROFILE%\bin\bws.exe` (e.g. `C:\Users\me\bin\bws.exe`), version **2.1.0** from [sdk-sm releases](https://github.com/bitwarden/sdk-sm/releases) (`bws-x86_64-pc-windows-msvc-*.zip`).
3. **User `Path`** — Includes `%USERPROFILE%\bin` so new shells find `bws` without a full path. **Existing** agent/IDE sessions may need a restart to pick up Path changes.

Quick check (PowerShell, do not print the token):

```powershell
[Environment]::GetEnvironmentVariable("BWS_ACCESS_TOKEN", "User") | ForEach-Object {
  if ($_) { "BWS_ACCESS_TOKEN: present len=$($_.Length)" } else { "BWS_ACCESS_TOKEN: MISSING" }
}
Get-Command bws | Select-Object Source
bws --version
```

### macOS hosts

1. Install `bws` (pick one):
   - Official install: `curl https://bws.bitwarden.com/install | sh` (see [Bitwarden Secrets Manager CLI](https://bitwarden.com/help/secrets-manager-cli/))
   - Or download the macOS zip from [sdk-sm releases](https://github.com/bitwarden/sdk-sm/releases) (`bws-macos-universal-*.zip` or arch-specific) into `~/bin` and ensure `~/bin` is on `PATH`.
2. Create/use machine account **`mac-codex`** and store its access token as **`BWS_ACCESS_TOKEN`** in **`~/.zprofile`** (not `~/.zshrc` — `.zprofile` is sourced by login shells, which is what GUI-launched agents and new Terminal windows both get; `.zshrc` alone is not enough):
   ```bash
   printf '\nexport BWS_ACCESS_TOKEN="paste-token-here"\n' >> ~/.zprofile
   source ~/.zprofile
   ```
3. Verify: `command -v bws && bws --version` and that `BWS_ACCESS_TOKEN` is set in the agent's process environment (see quick check below).

**This token has gone missing before** (found on 2026-08-05: `bws` was installed but `BWS_ACCESS_TOKEN` wasn't in `.zprofile`, `.zshrc`, `.zshenv`, `.profile`, or launchd's user environment — likely lost in a shell-profile reset or new-machine setup that didn't carry it over). If a fresh Claude/agent session on this Mac reports the token missing, don't assume it's still set somewhere unsearched — regenerate a new `mac-codex` token in the Bitwarden Secrets Manager web vault (Queenzone Development project → Machine accounts → `mac-codex`; old tokens can't be viewed again, only revoked) and re-run step 2 above. Agents should never try to read it out of Keychain or other credential stores directly — ask the user to set it and confirm.

**Quick check (macOS, do not print the token):**
```bash
if [ -n "$BWS_ACCESS_TOKEN" ]; then echo "BWS_ACCESS_TOKEN: present len=${#BWS_ACCESS_TOKEN}"; else echo "BWS_ACCESS_TOKEN: MISSING"; fi
bws --version
bws project list
```

## Common agent commands

Load the token into the **current** process, then call `bws` (works the same on Windows PowerShell and macOS once `bws` is on `PATH`):

```powershell
# Windows PowerShell / pwsh
$env:BWS_ACCESS_TOKEN = [Environment]::GetEnvironmentVariable("BWS_ACCESS_TOKEN", "User")
bws project list
bws secret list "1c16fd2d-4bfb-4eb7-8357-b49400233490"
```

```bash
# macOS / Linux shell
export BWS_ACCESS_TOKEN="${BWS_ACCESS_TOKEN:-$(printenv BWS_ACCESS_TOKEN)}"
# If only set in the login keychain/profile, ensure the agent shell sources it first.
bws project list
bws secret list "1c16fd2d-4bfb-4eb7-8357-b49400233490"
```

### Load legacy SQL connection for tools (never print the value)

```powershell
$env:BWS_ACCESS_TOKEN = [Environment]::GetEnvironmentVariable("BWS_ACCESS_TOKEN", "User")
$secrets = bws secret list "1c16fd2d-4bfb-4eb7-8357-b49400233490" --output json | ConvertFrom-Json
$cs = $secrets | Where-Object { $_.key -eq "ConnectionStrings__QueenZoneLegacy" } | Select-Object -First 1
if (-not $cs) { throw "ConnectionStrings__QueenZoneLegacy not found in Bitwarden project" }
$env:ConnectionStrings__QueenZoneLegacy = $cs.value
"loaded ConnectionStrings__QueenZoneLegacy len=$($cs.value.Length)"
# example: photo original-dimension inventory (issue #435)
# dotnet run --project src/QueenZone.Tools -- photo-dim-inventory
```

Related keys that may exist in the same project (names only): `ConnectionStrings__QueenZoneLegacyLive`, `ConnectionStrings__QueenZoneLegacyLocal`, `ConnectionStrings__BlobStorage`, `AzureAd__ClientSecret`, `OPENROUTER_API_KEY`, `QUEENZONE_SQL_EXPRESS_PROBE_PASSWORD`, `QUEENZONE_SQL_EXPRESS_PROBE_USERNAME`, etc.

`QUEENZONE_SQL_EXPRESS_PROBE_USERNAME` / `QUEENZONE_SQL_EXPRESS_PROBE_PASSWORD` are the paired SQL-login credentials (`queenzone_probe`) for connecting to the SQL Express mirror from a non-domain-joined host (e.g. the Mac runner reaching `glory11` over LAN, where Windows Integrated Security isn't usable) — see [issue #540](https://github.com/richardorchard/QueenZone.Modern/issues/540). The username secret was added 2026-08-05 after the password-only secret left the username undocumented anywhere; confirmed working from the Mac: `sqlcmd -S glory11 -U queenzone_probe -C -Q "SELECT 1"`.

### macOS SQL client tooling (sqlcmd)

Needed for any script that connects to the `glory11` SQL Express mirror from macOS (Windows Integrated Security doesn't apply here — use the `queenzone_probe` SQL login above instead):

```bash
brew tap microsoft/mssql-release https://github.com/Microsoft/homebrew-mssql-release
brew trust --taps microsoft/mssql-release   # required: Homebrew refuses third-party taps until trusted
HOMEBREW_ACCEPT_EULA=Y brew install msodbcsql18 mssql-tools18
```

**Use `HOMEBREW_ACCEPT_EULA`, not `ACCEPT_EULA`** — the wrong variable name doesn't error, it silently leaves the formula waiting on an interactive EULA prompt (`STDIN.gets`) that never resolves in a non-interactive shell, so the install just hangs indefinitely with no error output.

`sqlcmd` installs to `/opt/homebrew/opt/mssql-tools18/bin`, which isn't on `PATH` by default — add it in `~/.zprofile`:
```bash
export PATH="/opt/homebrew/opt/mssql-tools18/bin:$PATH"
```

Verify:
```bash
sqlcmd -S glory11 -U queenzone_probe -C -Q "SELECT 1 AS ok"
```

## Install / repair `bws` (Windows)

If `bws` is missing from PATH:

```powershell
$dest = Join-Path $env:USERPROFILE "bin"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$zip = Join-Path $env:TEMP "bws-windows.zip"
# Pin or bump version from https://github.com/bitwarden/sdk-sm/releases (bws-v* tags)
$ver = "2.1.0"
Invoke-WebRequest -Uri "https://github.com/bitwarden/sdk-sm/releases/download/bws-v$ver/bws-x86_64-pc-windows-msvc-$ver.zip" -OutFile $zip
Expand-Archive -Path $zip -DestinationPath $dest -Force
# Ensure User Path contains %USERPROFILE%\bin (once)
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (-not (($userPath -split ";") -contains $dest)) {
  [Environment]::SetEnvironmentVariable("Path", "$dest;$userPath", "User")
}
& "$dest\bws.exe" --version
```

Restart the IDE/agent terminal after changing User `Path`.

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| `bws` not recognized | CLI not installed or User `Path` not reloaded in this process |
| Auth / 401 from `bws` | `BWS_ACCESS_TOKEN` missing, wrong machine account for this OS, or expired token |
| `bw status` unauthenticated | Wrong CLI — use **`bws`**, not password-manager **`bw`** |
| Secret key not found | Wrong project id, or key renamed; re-list keys only (no values) |
| Agent on Mac can’t use Windows token | Expected — use `mac-codex` token on Mac hosts |

## See also

- `AGENTS.md` — short agent rules
- `README.md` — local development overview
- `docs/agent-handoff-cheatsheet.md` — multi-session handoffs
- [Bitwarden Secrets Manager CLI help](https://bitwarden.com/help/secrets-manager-cli/)
