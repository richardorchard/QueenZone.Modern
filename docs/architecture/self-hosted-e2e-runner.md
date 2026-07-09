# Self-Hosted Windows Runner For Playwright E2E Tests

The `e2e-test` job in `.github/workflows/ci.yml` runs the Playwright smoke suite (`tests/QueenZone.Web.E2E`) on a self-hosted Windows runner instead of `ubuntu-latest`. Self-hosted runner minutes are not billed, so this keeps the browser-level e2e suite out of the GitHub Actions minutes quota. `continue-on-error: true` on the job means a pull request can still merge if this machine is offline; treat e2e failures as a signal to investigate, not a hard CI gate, until the runner is reliably available.

## One-Time Setup On This Machine

1. In GitHub, go to the repository's **Settings > Actions > Runners > New self-hosted runner**, choose **Windows**, and copy the generated token/URL (it is single-use and short-lived; re-generate if it expires before you finish).
2. On this machine, open PowerShell and create a runner folder outside the repo, for example:

   ```powershell
   mkdir C:\actions-runner
   cd C:\actions-runner
   ```

3. Download and extract the runner package (use the exact command GitHub shows you on the "New self-hosted runner" page — the version number changes over time):

   ```powershell
   Invoke-WebRequest -Uri https://github.com/actions/runner/releases/download/v2.XXX.X/actions-runner-win-x64-2.XXX.X.zip -OutFile actions-runner.zip
   Expand-Archive -Path actions-runner.zip -DestinationPath .
   ```

4. Configure the runner against this repo, using the token from step 1:

   ```powershell
   .\config.cmd --url https://github.com/richardorchard/QueenZone.Modern --token <TOKEN_FROM_GITHUB>
   ```

   - Accept the default runner group.
   - Give it a recognizable name, e.g. `richard-win11-desktop`.
   - Add a label so the workflow can target it specifically if you ever run more than one self-hosted runner, e.g. `windows-local`. The workflow currently matches on the built-in `Windows` label, which `config.cmd` adds automatically.

5. Install it as a Windows service so it keeps listening for jobs without a logged-in session:

   ```powershell
   .\svc.cmd install
   .\svc.cmd start
   ```

   Run `.\svc.cmd status` any time to check it's listening. Use `.\svc.cmd stop` to pause it (e.g. if you want this machine's CPU/network back for other work) and `.\svc.cmd uninstall` to remove the service entirely.

## Machine Prerequisites

The runner executes the same steps as the workflow locally, so this machine needs:

- **.NET 10 SDK** (`dotnet --version` should report `10.0.x`) — already installed for normal development on this machine.
- **Playwright browser binaries** — the workflow installs these itself via `playwright.ps1 install chromium` on each run, cached under `%USERPROFILE%\AppData\Local\ms-playwright`. No manual install needed, but the first run after a Playwright package bump will re-download the browser.
- **Outbound internet access** to `github.com`, `actions.githubusercontent.com`, and `cdn.playwright.dev` (for the runner itself and for browser downloads).
- **Port 5099 free** — the e2e job binds the published app to `http://127.0.0.1:5099`. If something else is using that port, the "Wait for app to be ready" step will time out.

## Security Notes

A self-hosted runner executes whatever code is in the triggered workflow run on this machine, including from any commit pushed to a branch that triggers `pull_request` or `push`. Because this repo currently only has trusted agent-prefixed branches and no public external contributors, the default `pull_request` trigger is acceptable. If this repository ever accepts pull requests from forks or untrusted contributors, switch the `e2e-test` job to `pull_request_target` with an explicit approval gate, or restrict it to `workflow_dispatch`/`push` only, before keeping a self-hosted runner registered.

## Running It Manually

The workflow also accepts `workflow_dispatch`, so you can trigger `e2e-test` (along with the rest of CI) on demand from the **Actions** tab without waiting for a push, useful for checking the runner is online after machine restarts or service updates.

### Local run (against a published Testing app)

```powershell
dotnet build tests/QueenZone.Web.E2E/QueenZone.Web.E2E.csproj --configuration Release
.\tests\QueenZone.Web.E2E\bin\Release\net10.0\playwright.ps1 install chromium
dotnet publish src/QueenZone.Web/QueenZone.Web.csproj --configuration Release --output ./e2e-app
# Terminal A:
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5099"
$env:ASPNETCORE_CONTENTROOT = (Resolve-Path .\e2e-app).Path
.\e2e-app\QueenZone.Web.exe
# Terminal B:
$env:E2E_BASE_URL = "http://127.0.0.1:5099"
$env:E2E_ADMIN_EMAIL = "admin@test.local"
$env:E2E_ARTIFACT_DIR = "test-results/e2e"
dotnet test tests/QueenZone.Web.E2E/QueenZone.Web.E2E.csproj --configuration Release --no-build
```

Failed tests write screenshots (`.png`) and Playwright traces (`.zip`) under `test-results/e2e/`. Open a trace with:

```powershell
npx playwright show-trace test-results/e2e\<name>.zip
```

## Troubleshooting

- **Job stuck in "Waiting for a runner to pick up this job"**: the service isn't running. Run `.\svc.cmd status` in the runner folder and `.\svc.cmd start` if needed.
- **Job fails immediately with a missing SDK/tool error**: re-check the prerequisites above; the self-hosted runner uses whatever is already on `PATH` on this machine, unlike GitHub-hosted runners which come preconfigured.
- **Stale Chromium after a Playwright version bump**: delete `%USERPROFILE%\AppData\Local\ms-playwright` and let the next run re-download it.
- **Checkout fails with `EPERM: operation not permitted, unlink ... QueenZone.Web.exe`**: a previous run's "Stop app" step failed to terminate the real `QueenZone.Web.exe` process (the child-process PID lookup in "Start app in background" can occasionally miss it), leaving it orphaned and holding its own exe file locked. The `e2e-test` job now kills any leftover `QueenZone.Web` process by image name both before checkout and at the end of the job, so this should self-heal on the next run. If it still happens, the orphaned process may be running with higher privileges than the runner's own session (this has happened when a run was started or affected by an elevated/admin process) — open an elevated PowerShell on this machine and run `Stop-Process -Name QueenZone.Web -Force`, then re-run the job.
