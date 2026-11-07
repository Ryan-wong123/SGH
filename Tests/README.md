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

Both jobs use the editor-only `unityci/editor:ubuntu-<UnityVersion>-base-3` image,
with the version read from `ProjectSettings/ProjectVersion.txt`. Neither suite
builds a standalone player, so Linux IL2CPP modules are unnecessary. Before
checkout, the workflow frees unused host SDKs and Docker images on the disposable
GitHub-hosted runner. This prevents the previous Docker extraction failure
(`no space left on device`, exit 125). The cleanup refuses to run on a self-hosted
runner and never removes project files. Disk usage is logged before and after
Unity. A future standalone build needs a suitable build image and a separate cache
namespace.

Before importing, CI runs `python3 Tests/Check-AssetPaths.py` to reject tracked
asset paths (including implicit folders) that differ only by casing. Windows can
hide these collisions while Linux checks out both paths. The 2 November archive
`logs_112061134117.zip` contains passing XML for all 185 Edit Mode and 13 Play Mode
tests, but both editors exit 3 during cleanup because `Assets/UI/Activity 7` and
`Assets/UI/activity 7` collide. The subsequent preflight run still found those
paths because the case-only renames were absent from the pushed commit. The
physical folder is now named `Assets/UI/Activity7`, including its folder `.meta`,
so Windows Git clients retain the rename when staging files again. All 26 image
and file-metadata contents plus the folder GUID are preserved byte-for-byte.
The preflight
reports future collisions before the expensive Unity run; Unity errors are not
suppressed and failed jobs are not converted into successes.

The 2 November Play Mode log failed before any NUnit test ran: Crunch reported
`crn_threading_pthreads.cpp(194)` / `sem_wait() or sem_timedwait() failed` while
importing a texture, then the editor crashed with exit 255 and produced no test
XML. The Edit Mode job in that archive was canceled during startup/import; it
contains no assertion result either. CI now uses `-overrideTextureCompression
forceUncompressed -overrideMaxTextureSize 256 -job-worker-count 2` in both jobs.
These [Unity import overrides](https://docs.unity3d.com/6000.3/Documentation/Manual/EditorCommandLineArguments.html)
bypass Crunch and limit texture memory/worker pressure for the automated fixtures.
The source texture `.meta` files are unchanged. Import caches use the distinct
`unity-base-fast-import-v3` prefix so incomplete/old compressed imports are not
restored. This suite does not verify full-resolution rendering or production
compression; those remain separate manual/build checks. A fresh GitHub run is
required to confirm the Linux crash workaround on the runner.

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
Successful delivery, exact-limit acceptance, SMTP rejection, cancellation and rate limiting now have local integration tests:

```powershell
dotnet test backend/SghCsvEmailApi.Tests/SghCsvEmailApi.Tests.csproj
```

They bind fake SMTP on loopback, use synthetic CSV only, and cannot forward mail externally. The Unity direct SMTP route remains separate.

## Extended automation

See [AutomatedCoverage.md](AutomatedCoverage.md) for coverage and execution boundaries. The default PR Unity job still runs the isolated EditMode/PlayMode assemblies. Backend tests have their own PR workflow.

All extended suites now run automatically on PR opened/synchronize/reopened/ready-for-review events alongside the fast suite. No repository opt-in variable is required. Manual dispatch remains available to rerun `journey`, `visual`, `soak`, `standalone`, `live`, `rendered` or `all`. Missing licensing/Photon secrets fail the selected jobs explicitly; fork PRs cannot receive these secrets, so a maintainer must run them from a trusted repository branch. The workflow uses pull_request, never pull_request_target.

Live tests require dedicated test-only Actions secrets `SGH_PHOTON_APP_ID` and `SGH_PHOTON_CHAT_APP_ID`. Optional repository variable `SGH_PHOTON_REGION` selects a region (default `asia`, Photon's Singapore region code; legacy `sg` is normalized to `asia`). These must be separate from production applications. The workflow creates a temporary ignored configuration outside Assets for the Docker test process and deletes it afterward. Never commit it or upload it as an artifact. Rooms, users and messages are synthetic, randomized and isolated by an automation application version.

For local live execution, set the corresponding environment variables in the editor process before launch. Missing IDs fail the selected live suite instead of silently using production settings. Never paste IDs/passwords into test source or reports.

System tests need a graphics context (do not add `-nographics`). Real scene tests isolate Chat and CSV delivery; they must run in a separate Unity process. The standalone suite uses the project's Mono backend with Lobby/GameScene included. It does not change project build settings. Screenshots and smoke-budget metrics are saved under `TestResults/System`; these contain synthetic state only.

Local extended runner (close Unity first):

```powershell
./Tests/Run-ExtendedUnityTests.ps1 -Suite Journey
./Tests/Run-ExtendedUnityTests.ps1 -Suite Visual
./Tests/Run-ExtendedUnityTests.ps1 -Suite Soak
./Tests/Run-ExtendedUnityTests.ps1 -Suite Standalone
# Live requires the dedicated IDs in this process environment:
./Tests/Run-ExtendedUnityTests.ps1 -Suite Live
```

A failed product invariant is a test failure, not a reason to relax assertions. The coverage manifest distinguishes pending external execution and unsupported lifecycle features from verified passing suites.


## Two rendered clients and desktop device input

The `rendered` PR job builds a separate automation player, launches two independent processes under Xvfb/openbox, and uses xdotool to send OS W movement, mouse camera motion, Q cursor unlock and a pointer click to the actual Chat collapse button in each window. Both processes must report measured movement/camera/UI changes, two authored avatars, real scene Chat delivery/unread/channel behavior, all 11 stages and two consecutive sessions. Players alternate puzzle turns, acquire active card ownership through PUN, and exit the ending independently. Any error log, timeout, missing window, missing input evidence, wrong stage count, failed process exit or stale report directory fails the run. Six Python contract tests verify the orchestrator's pass/fail and cleanup rules.

The player builds through `SGH.Tests.RenderedPlayerBuild.Linux` (CI) or `.Windows` (local build verification). It includes a minimal authored `RenderedBootstrap` scene and test assemblies; normal production builds do not include this harness. Dedicated IDs are injected from an external configuration file, and fallback reconnect settings are fenced to the dedicated applications in runtime memory. SMTP credentials are cleared and CSV export is suppressed in this synthetic journey.

The full rendered orchestration runs on Linux/X11 and needs real dedicated Photon/Chat secrets. Its desktop input checks do not certify Android/iOS hardware, touch controls, GPU vendors or physical screen quality. The five aspect ratios are rendered viewport tests, including every authored multipart puzzle page; they are not a claim that phone hardware was exercised. The project currently uses desktop keyboard/mouse controls.
