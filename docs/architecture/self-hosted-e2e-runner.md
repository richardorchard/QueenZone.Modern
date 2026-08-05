# Self-Hosted Runners For Playwright E2E Tests

The `e2e-test` job in `.github/workflows/ci.yml` runs the Playwright smoke suite (`tests/QueenZone.Web.E2E`) on whichever online, idle self-hosted runner has the `e2e` label. The repository currently has Windows and macOS runners, and GitHub assigns the job to the first matching runner that accepts it. Self-hosted runner minutes are not billed, so this keeps the browser-level e2e suite out of the GitHub Actions minutes quota.

The job is a required pull-request merge gate. If both runners are offline, the job remains queued and the pull request cannot merge. It is not a deploy gate: `.github/workflows/deploy.yml` does not rerun e2e after merge.

## One-Time Windows Setup

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
   - Add the custom `e2e` label. The workflow targets `[self-hosted, e2e]`; the built-in `Windows` label alone will not pick up this job.

5. Install it as a Windows service so it keeps listening for jobs without a logged-in session:

   ```powershell
   .\svc.cmd install
   .\svc.cmd start
   ```

   Run `.\svc.cmd status` any time to check it's listening. Use `.\svc.cmd stop` to pause it (e.g. if you want this machine's CPU/network back for other work) and `.\svc.cmd uninstall` to remove the service entirely.

## One-Time macOS Setup

1. In GitHub, go to the repository's **Settings > Actions > Runners > New self-hosted runner**, choose **macOS**, and follow the displayed download and extraction commands. The package version and registration token change over time.
2. From the extracted runner directory, configure the runner and add the required label:

   ```bash
   ./config.sh --url https://github.com/richardorchard/QueenZone.Modern --token <TOKEN_FROM_GITHUB> --labels e2e
   ```

3. Install and start the runner service so it remains available without an interactive shell:

   ```bash
   ./svc.sh install
   ./svc.sh start
   ```

   Use `./svc.sh status`, `./svc.sh stop`, and `./svc.sh uninstall` to inspect or manage it.

## Machine Prerequisites

The runner executes the same steps as the workflow locally, so this machine needs:

- **.NET 10 SDK** (`dotnet --version` should report `10.0.x`) available on the runner service's `PATH`.
- **PowerShell 7 on macOS** (`pwsh`; for example, `brew install --cask powershell`) so the workflow can invoke Playwright's generated installer script.
- **Playwright browser binaries** — the workflow installs Chromium itself on each run. No manual browser install is needed, but the first run after a Playwright package bump downloads it again.
- **Outbound internet access** to `github.com`, `actions.githubusercontent.com`, and `cdn.playwright.dev` (for the runner itself and for browser downloads).
- **Port 5099 free** — the e2e job binds the published app to `http://127.0.0.1:5099`. If something else is using that port, the "Wait for app to be ready" step will time out.

## Security Notes

A self-hosted runner executes code from pull requests on the machine. Because this repository currently only has trusted agent-prefixed branches and no public external contributors, the `pull_request` trigger is acceptable. Before accepting untrusted contributions, redesign the e2e trigger and approval boundary so unreviewed fork code cannot execute on either self-hosted runner.

## Running It Manually

The workflow also accepts `workflow_dispatch`, so you can trigger `e2e-test` (along with the rest of CI) on demand from the **Actions** tab, useful for checking that at least one matching runner is online after machine restarts or service updates.

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
# Pin to Deterministic (same as the CI merge gate). RealData is the nightly suite.
dotnet test tests/QueenZone.Web.E2E/QueenZone.Web.E2E.csproj --configuration Release --no-build --filter "TestCategory=Deterministic"
```

Failed tests write screenshots (`.png`) and Playwright traces (`.zip`) under `test-results/e2e/`. Open a trace with:

```powershell
npx playwright show-trace test-results/e2e\<name>.zip
```

## Troubleshooting

- **Job stuck in "Waiting for a runner to pick up this job"**: neither matching runner is available. Confirm each runner is online in **Settings > Actions > Runners**, has the `e2e` label, and has its service running (`.\svc.cmd status` on Windows or `./svc.sh status` on macOS).
- **Job fails immediately with a missing SDK/tool error**: re-check the prerequisites above; the self-hosted runner uses whatever is already on `PATH` on this machine, unlike GitHub-hosted runners which come preconfigured.
- **Stale Chromium after a Playwright version bump**: remove the runner account's Playwright browser cache and let the next run re-download it (`%USERPROFILE%\AppData\Local\ms-playwright` on Windows; `~/Library/Caches/ms-playwright` on macOS).
- **Checkout fails with `EPERM: operation not permitted, unlink ... QueenZone.Web.exe`**: a previous run's "Stop app" step failed to terminate the real `QueenZone.Web.exe` process (the child-process PID lookup in "Start app in background" can occasionally miss it), leaving it orphaned and holding its own exe file locked. The `e2e-test` job now kills any leftover `QueenZone.Web` process by image name both before checkout and at the end of the job, so this should self-heal on the next run. If it still happens, the orphaned process may be running with higher privileges than the runner's own session (this has happened when a run was started or affected by an elevated/admin process) — open an elevated PowerShell on this machine and run `Stop-Process -Name QueenZone.Web -Force`, then re-run the job.
