# Verification — 1 November 2026

## Changes

- Central event allocation: analytics 61, clue counts 62, crossword text 63,
  crossword result 64, word search 65. Retired 41-43 are ignored.
- Exact typed payload validation; game version suffix `sgh2` partitions old clients.
- Insulin room revision/part/deadline snapshot, master-only conditional scheduling
  and completion, reset invalidation and dropdown reset, room replay on recovery.
- Offline insulin coroutine cancellation and revision checks on reset/disable.
- Former three known-gap tests are regular regressions. Added malformed-event,
  state/deadline, and Photon offline-room integration cases.

## Verification scope

Production and both Unity test assemblies compile with `dotnet build --no-restore`.
`git diff --check` passes.

Unity 6000.3.11f1 in-editor suite results, 1 November 2026:

| Suite | Passed | Failed | Skipped | Run (UTC) |
| --- | --- | --- | --- | --- |
| Edit Mode | 185 | 0 | 0 | 10:46:57-10:47:00 |
| Play Mode | 13 | 0 | 0 | 10:48:36-10:48:40 |

All three former failing diagnostics pass as normal regressions. XML is in ignored
`TestResults/sgh-EditMode.xml` and `TestResults/sgh-PlayMode.xml`. Initial runs
exposed two new fixture setup errors (Photon ParameterDictionary construction and
Unity setup ordering); these were corrected before the successful full runs.

The user authorized saving the existing unsaved Lobby edits before execution.
Unity saved the scene and restored Lobby after each temporary test scene. Photon
also appended `RPC_RequestInsulinAdvance` to its serialized RPC list; keep that
asset with the new RPC so clients share the same RPC-name inventory. No vendor
source was changed. No cloud connection or email was used by these tests.

## Required executable checks

Both automated suites above have passed. Run two upgraded clients (`1.0-sgh2`) in the same room and verify:

1. Crossword typing/results and analytics do not trigger each other's receiver;
   clue waiting-count updates and word-search selections remain independent.
2. A non-master lock owner submits the correct syringe, both clients show part 2
   after two network seconds, and repeated submissions do not extend that delay.
3. Reset during that delay keeps both clients on part 1 beyond the old deadline,
   resets the dropdown, and allows a fresh submission. Repeat near the deadline.
4. Disconnect the owner/master during the delay; remaining master completes it.
   Rejoin restores the room part. Repeat reset during master migration.
5. Re-enter the puzzle panel, retry after an incorrect answer, and create a second
   room in the same process. The previous room's pending state must not leak.

Offline fixtures exercise Photon room APIs without cloud traffic or email, but
cannot establish live server compare-and-swap ordering, migration, or rejoin.
