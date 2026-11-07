# Verification — 30 October 2026

Unity 6.3 LTS `6000.3.11f1`, existing SGH project, Windows.

| Execution | Result |
| --- | --- |
| SGH Edit Mode regression suite | **163 passed, 0 failed** |
| SGH Play Mode regression suite | **8 passed, 0 failed** |
| Edit Mode known-gap diagnostics | **2 failed**, reproducing event 41 and event 42 collisions |
| Play Mode known-gap diagnostic | **1 failed**, reproducing insulin reset reopening part 2 after the pending delay |
| `dotnet build Assembly-CSharp.csproj --no-restore` | Passed; initial full compile had 35 existing warnings, 0 errors; final incremental compile had 0 warnings, 0 errors |
| Generated Edit Mode/Play Mode test project builds | Passed |
| PowerShell runner/API script syntax parsing | Passed |
| `git diff --check` | Passed |

Raw NUnit XML is in ignored `TestResults/sgh-EditMode.xml`,
`sgh-PlayMode.xml`, `sgh-EditMode-diagnostics.xml` and
`sgh-PlayMode-diagnostics.xml`. Diagnostic failure is expected against the current
runtime, but still represents an unresolved game defect.

The regular suites cover answer rules for all eleven puzzle managers,
multipart page/reset behavior, grid selection, input replication sequencing,
inactive-player eligibility, card hierarchy paths, room settings, synthetic
analytics aggregation/CSV escaping, inventory, doors, keypad behavior,
presentation handoff, popup ordering and offline-clock timer expiry while
timescale is zero. Scene contracts inspect actual Lobby/GameScene serialization
through Unity preview scenes, including stage bindings, answer configuration,
routes, shared Close and avatar references. The room-list layout contract passed.

Play Mode tests ran in Unity's isolated temporary test scene. They did not run
two Photon clients through GameScene. No live SMTP email was sent, no gameplay
CSV was used as a fixture, and the optional HTTP API validation script was not
executed against a deployment. Batch launching was not exercised because this
project remained open in the editor; the same test assemblies were executed by
the in-editor runner.

The manual scenarios in `RegressionScenarios.md` remain **Not run**, including
two-client progression/master migration/rejoin, drag ownership transport,
all aspect ratios, real input/animation, full story/ending traversal, repeat
sessions and backend/direct-SMTP delivery. These checks cannot be inferred from
the passing offline suites.

Changes are tests, test runners, documentation and test-output ignore rules.
Production behavior and scene/prefab assets were not changed. Generated tracked
`obj` artifacts and Unity-generated solution edits were restored after builds;
the working tree was clean before this work.
