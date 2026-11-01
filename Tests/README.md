# SGH regression tests

Coverage is based on `PRD.md`, `architecture.md`, project-owned scripts and the
enabled Lobby/GameScene assets. Vendor packages and artwork are dependencies,
not additional game scenarios. No finite suite proves every possible scenario.
Network transport, rendering, device input and delivery require the executable
manual scenarios in [RegressionScenarios.md](RegressionScenarios.md).

## Run in the currently open Unity editor

Use **SGH > Tests > Run Edit Mode suite**, then **Run Play Mode suite**.
Alternatively use **Window > General > Test Runner**, selecting the SGH test
assemblies. The menu commands exclude `KnownGap`; selecting an assembly directly
in Unity can run explicit diagnostic tests as well.

Tests live in `Assets/Scripts/Tests`. Test assemblies use reflection to exercise
the existing `Assembly-CSharp` components without moving production scripts to
an asmdef and risking serialized/network type compatibility. Fixtures use
synthetic data, isolated objects, and preview scenes. Scene checks do not save or
replace the open scene. Play Mode tests use Unity's temporary test scene; they do
not connect two clients or walk the real GameScene.

The in-editor commands write NUnit XML to ignored `TestResults/sgh-EditMode.xml`
and `TestResults/sgh-PlayMode.xml`. Keep results with the build/commit under test.
Generated `SGH.Tests.*.csproj` files are ignored; Unity recreates them.

## Batch / CI

### GitHub Actions

[Unity tests](../.github/workflows/unity-tests.yml) runs when a pull request is
opened, updated, reopened, or marked ready for review. It can also be started
manually from Actions. Separate Edit Mode and Play Mode jobs use the Unity version
in `ProjectSettings/ProjectVersion.txt` (currently `6000.3.11f1`), select the SGH
test assemblies, and exclude `KnownGap`, matching the local runner. The fixed-issue
regressions are included. A failing test fails its job; both jobs run even if one
fails. Imports are cached and XML/results/logs are uploaded for 1 day, including
after test failures.

The test runner is pinned to `game-ci/unity-test-runner@v4.3.2`, the Docker-based
release. The floating `@v4` moved to a CLI wrapper that passed
`--no-coverageEnabled` to CLI v0.1.72, which rejected the argument before either
suite started. The pinned release does not support the `coverageEnabled` input,
so it is omitted. Its built-in Unity command still includes coverage arguments;
this project does not install the Code Coverage package or require a coverage
report. See the [upstream release notes](https://github.com/game-ci/unity-test-runner/releases/tag/v4.3.2)
before upgrading the action. Both matrix jobs use the same pinned implementation.

Before the first run, configure repository **Settings > Secrets and variables >
Actions** with `UNITY_EMAIL`, `UNITY_PASSWORD`, and either `UNITY_LICENSE` (an
activated Personal license file) or `UNITY_SERIAL` (Pro), following the
[GameCI licensing instructions](https://game.ci/docs/github/activation/).
Never commit license files or account credentials. Jobs run sequentially to avoid
acquiring two license seats concurrently. The workflow uses a read-only GitHub
token; job status provides the PR checks and downloadable artifacts provide details.

Fork pull requests and Dependabot PRs normally cannot access these Actions secrets;
the workflow reports missing licensing and fails rather than claiming tests passed.
Run an approved change from a trusted repository branch to supply the secrets.
Do not switch to `pull_request_target` to run untrusted PR code with credentials.
No Photon account or SMTP credentials are required: these are the automated
fixture/offline-room suites, not the live two-client or email-delivery scenarios.
The workflow must be committed and pushed before GitHub can execute it. Configure
`Unity EditMode` and `Unity PlayMode` as required branch checks if desired.

### Local batch runner

Close this project's Unity editor first, then from the repository root:

```powershell
./Tests/Run-UnityTests.ps1
./Tests/Run-UnityTests.ps1 -Mode EditMode
./Tests/Run-UnityTests.ps1 -Mode PlayMode -FixedIssues
dotnet build Assembly-CSharp.csproj --no-restore
git diff --check
```

Override `-Unity` if the editor is installed elsewhere. The batch runner exits
nonzero for failed tests and writes XML/logs to `TestResults`. A `dotnet build`
compiles C# only; it does not execute Unity tests. Restore build-generated tracked
`obj` artifacts only when they were clean before running the build.

## Fixed-issue regressions

The **Run fixed-issue Edit Mode regressions** and **Run fixed-issue Play Mode
regressions** menu commands select `RegressionFixes`. These tests also run in the
regular suites. The former three failing diagnostics now assert distinct event
codes and cancellation of insulin's pending delay after reset. Additional cases
cover malformed payloads, reset revisions, deadline recovery, duplicate requests,
and ownership checks using Photon offline rooms. Offline rooms do not verify
server compare-and-swap ordering or real master migration; use two clients for
those checks. Focused runs write `sgh-EditMode-fixes.xml` / `sgh-PlayMode-fixes.xml`.

Keep multiplayer recovery/session-lifecycle gaps in the manual matrix visible.
Do not mark those scenarios passed based on offline fixture tests.

## Optional backend

Against a dedicated test deployment, use:

```powershell
./Tests/Test-EmailApiValidation.ps1 -BaseUri https://your-test-host.example.invalid/
# Add -IncludeRateLimit only on an isolated test instance in a fresh limiter window.
```

This validates health and rejected synthetic requests only; it never deliberately
submits a valid CSV, reads gameplay exports, or exercises SMTP. Redirects are
disabled so an HTTP/TLS deployment problem cannot silently change the target.
Successful delivery, exact-limit acceptance, SMTP failures and cancellation must
use the manual backend cases with verified test recipients after sender constants
have been replaced by secure test configuration. The Unity direct SMTP route is
separate from this API.
