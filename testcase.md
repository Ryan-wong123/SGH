> Update, 1 November 2026: the three earlier failing diagnostics below now have
> runtime fixes and normal `RegressionFixes` tests. Analytics/clue/crossword/word
> search use unique codes 61�65 with typed payload validation and protocol version
> `sgh2`. Insulin uses a master-written revision/part/deadline snapshot; reset
> invalidates pending work. Historical failure descriptions document the original
> reproduction; the expected result after these fixes is that all three pass.
> Verified in Unity on 1 November: 185 Edit Mode and 13 Play Mode tests passed,
> including all three former failing diagnostics.
> See `Tests/README.md` for running the focused regressions. Two-client reset,
> disconnect/rejoin and master-migration checks remain necessary.

# SGH detailed test cases

Prepared: **1 November 2026**. Requirements baseline: [PRD.md](PRD.md) and
[architecture.md](architecture.md), documented 8 September 2026. This expands
every scenario described in the preceding chat and [scenario matrix](Tests/RegressionScenarios.md).
Stage numbers refer to `PuzzleStateManager`, not activity-folder or panel names.

## 1. Scope, evidence, and execution rules

The cases cover Lobby setup, all eleven puzzle managers and multipart pages,
Photon coordination, timers, controls, story/UI, chat, analytics, optional email
API, scene wiring, and three known-defect diagnostics. Game answer strings are
software acceptance data, not independently validated clinical guidance.

The following is **historical evidence from 30 October 2026**, not a new run on
the document preparation date:

| Test execution | Recorded result | What that result establishes |
| --- | --- | --- |
| Edit Mode regression assembly | 163 passed, 0 failed | Covered offline methods and serialized scene contracts |
| Play Mode regression assembly | 8 passed, 0 failed | Covered behavior in Unity's isolated temporary test scene |
| Edit Mode diagnostics | 2 failed | Event 41 and 42 allocation collisions reproduced |
| Play Mode diagnostic | 1 failed | Reset during insulin delay reopened part two |
| Two-client acceptance, aspect-ratio walkthrough, actual email/API delivery | Not run | No pass claim can be made |

See [verification record](Tests/Verification-2026-10-30.md). Every manual case
below starts at **Not run**. Related automated coverage is supporting evidence
only; it does not convert a manual network case to Passed. An implementation
limitation is described separately from the desired acceptance behavior.

### 1.1 Environment and test data

1. Use the same build/commit on two independently identified clients, A and B.
   A may be Unity Editor and B a development build. Add client C for three-voter
   and round tests. Do not copy Photon user IDs between clients.
2. Use an isolated test room such as `SGH-QA-<run-id>`. Record actor numbers and
   which actor is master. Repeat shared cases after switching master to B.
3. Keep one active peer present for ordinary rejoin tests. Test an empty room
   separately; a cached name does not guarantee that the room or actor survives.
4. Use invented names, `sender@example.invalid`, `recipient@example.invalid`,
   and a clearly synthetic password for offline form tests. Those addresses do
   not deliver mail. Live delivery requires separately verified test accounts.
5. Prevent incidental mail during gameplay tests: use synthetic runtime values
   and a test environment, not real organiser credentials. Never use real
   gameplay CSVs as fixtures or include credentials/personal data in evidence.
6. Enter stages through normal gameplay for acceptance. A test harness may
   arrange a stage, ownership, or deadline for isolated reproduction, but record
   that setup. Do not edit source-controlled scenes to reach a test condition.
7. Record normal-latency and delayed/disconnected runs separately. A temporary
   disconnect leaves an inactive actor; intentional Leave is a distinct case.

### 1.2 Repetition and evidence

For each shared scenario, compare A and B at the action, during any delay, and
after completion. Repeat with A/B as attempt owner and master, at two players
and the configured maximum where applicable. Do not infer submission authority
from button visibility alone, or card ownership from puzzle-lock ownership.

Capture stage/part, panels, relevant control values, timer deadline, actor/master,
lock owner, round, rewards and vote counts. Capture sanitized exceptions and
before/after screenshots when relevant. After a failure, save the reproduction
details before resetting. Restore the test room/state between independent cases.

| Result | Meaning |
| --- | --- |
| Pass | All stated expected results observed in the recorded environment |
| Fail | At least one required result violated; include actual behavior |
| Not run | No execution evidence collected for this variant |
| Blocked | A prerequisite prevented execution; identify it |

### 1.3 Visual configurations

| Configuration | Resolution | Main risk |
| --- | --- | --- |
| Desktop 16:9 | 1920 x 1080 | Reference layout and pointer interaction |
| Desktop 16:10 | 1920 x 1200 | Vertical anchors and panel sizing |
| Ultrawide | 3440 x 1440 | Horizontal stretch, spacing, alignment |
| Tablet portrait | 768 x 1024 | Clipped panels, text wrapping, inaccessible controls |
| Mobile landscape | 1280 x 720 | Limited height, small controls, overlay collisions |

These are layout checks. Passing them does not establish touch-control support.

## 2. Home, rooms, loading, identity, and controls

### L01 — Username validation and trimming

**Purpose:** Prevent an unusable identity and confirm the first form step.
**Precondition:** Fresh Lobby at username entry.

1. Submit an empty field, then a whitespace-only field.
2. Confirm each rejection leaves the form at username entry and permits editing.
3. Enter `  Synthetic A  ` and continue once.
4. Complete later steps and inspect the resulting visible name.

**Expected:** Blank values do not advance. The valid name is trimmed to
`Synthetic A`, advances to sender entry once, and is used consistently afterward.
**Evidence:** Form-step screenshots and displayed player name; no real identity.

### L02 — Sender and recipient address validation

**Precondition:** L01 completed; sender step active. Repeat the data set at recipient entry.

1. Submit empty, whitespace, and `not-an-email`.
2. Submit `Test Person <sender@example.invalid>` to exercise display-name rejection.
3. Submit a padded plain address, ` sender@example.invalid `.
4. At recipient entry, repeat with `recipient@example.invalid`.

**Expected:** Invalid and display-name forms are rejected without skipping a
step. Valid plain addresses are trimmed, stored only in runtime memory, and
advance exactly once. Syntax acceptance does not establish a deliverable mailbox.

### L03 — Password masking, normalization, and credential containment

**Precondition:** App-password step active; use synthetic values only.

1. Submit blank and whitespace-only passwords; verify rejection.
2. Type a synthetic spaced value and inspect the field's password masking.
3. Continue to recipient entry; verify that field uses ordinary email display.
4. In an isolated fixture, retrieve credentials to check space removal, then clear
   them. Inspect property names/logs/assets for accidental credential storage.

**Expected:** Password is masked, blank rejected, and spaces removed. Retrieval
requires sender/password/recipient together; Clear removes all three. No value is
written to Photon properties, saved assets, documentation, or test evidence.

### L04 — Return to Lobby and second home-entry session

**Precondition:** Complete one test session; keep the process running.

1. Exit to Lobby through the normal flow.
2. Re-enter home information using a different synthetic name.
3. Create/join a second room and inspect labels, active panels, and cursor focus.
4. Repeat opening/closing the form and compare callbacks/logs with the first run.

**Expected:** Fresh usable entry state, no stale identity or duplicate UI action,
and correct cursor/input focus. Record persistent-manager and analytics lifecycle
defects separately; a fresh process must not conceal second-session failures.

### L05 — Room-name validation, capacity, password, and lifetimes

**Precondition:** Lobby connected; create-room UI available.

1. Submit an empty room name.
2. Create a named room, then attempt a name already present in the room cache.
3. Create a unique room for each configured capacity choice.
4. Inspect room options in the harness: capacity, password, player/empty TTL and
   lobby-exposed metadata. Repeat password generation over multiple samples.

**Expected:** Empty/duplicate names rejected; valid form remains usable. Password
is six characters from `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`. Player TTL is
600000 ms; empty-room TTL is 300000 ms. Credentials are not room metadata.

### L06 — Multiple room entries, refresh, and stale selection

**Precondition:** At least three isolated discoverable rooms.

1. Select each entry and confirm the selected-room label changes correctly.
2. Resize/scroll the list; refresh several times.
3. Remove, hide, close, or fill a selected room using another test client.
4. Allow room-list callbacks to arrive and try selection/confirmation again.

**Expected:** Entries do not overlap; all remain individually selectable. Cache
and UI reflect availability/capacity changes, with stale selection handled.
Full-room join failure must leave a usable UI; do not assume full rooms must be
hidden unless that behavior is specified by the current implementation.

### L07 — Password join validation and room disappearance

**Precondition:** A waiting room with a known synthetic room password.

1. Select it and submit wrong and empty passwords.
2. Submit the correct password with leading/trailing whitespace.
3. In a separate run, remove the selected room immediately before confirmation.
4. Retry selection/join after each rejection.

**Expected:** Wrong password denied; trimmed correct password joins. Missing
selection/unavailable room gives usable feedback without an orphan panel or
frozen button. This lobby-visible client password is a convenience gate, not
secure authentication or proof of resistance to modified clients.

### L08 — Waiting-room state and master-only start

**Precondition:** A creates; B joins; both remain active.

1. Compare names, population, capacity, and password displays on both clients.
2. Inspect B's Start availability; exercise a non-master Start invocation in a harness.
3. Let A start without an all-ready workflow.
4. Observe both scene loads and room `GameStarted`, visibility, and openness.

**Expected:** Non-master cannot initiate the shared start. Master starts both
clients into GameScene; room remains open/visible and marked started. No all-ready
requirement or exclusive role reservation should be invented by the test.

### L09 — Creation retry and random-join fallback

**Precondition:** Dedicated harness can inject one create/join failure.

1. Request a room and make its first creation fail.
2. Observe the retry after its configured delay, checking cached name/capacity/password.
3. Separately inject random-join failure to exercise unnamed-room fallback.
4. After success, inspect organiser actor assignment and connection attempts.

**Expected:** Retry preserves intended options. Fallback has correct TTLs and
sets organiser only after a valid actor is assigned. Derived networking managers
must not initiate competing reconnect flows. Record error feedback/retry usability.

### L10 — Avatar selection, duplicates, and fallback

**Precondition:** Waiting room; repeat in fresh stage-entry runs.

1. Select Doctor, Nurse, then Therapist and inspect each highlight/property.
2. Have A and B select the same role and start.
3. In isolated setup, remove selection; repeat with out-of-range integer values.
4. Compare instantiated prefab with the requested/fallback index.

**Expected:** Doctor=0, Nurse=1, Therapist=2; exactly selected role highlighted.
Duplicate roles are allowed and have no distinct exclusive abilities. Missing or
invalid supported integer selection falls back to Doctor; malformed property types
are an additional robustness probe, not covered by that fallback guarantee.

### L11 — Loading UI across load speeds and repeated sessions

**Precondition:** Two clients at room start; test normal and delayed asset loading.

1. Start GameScene and watch loading panel, random tip and progress fill.
2. Compare clients when one finishes substantially sooner.
3. Wait for loaded-scene UI to settle, then return and repeat in the same process.
4. Inspect for duplicate scene callbacks, null references, and stuck overlays.

**Expected:** Loading UI remains usable and disappears after load. Existing fade
loops calculate alpha without applying it; record this limitation rather than
asserting a completed visual fade solely because time elapsed.

### L12 — Actor-order spawning and avatar duplication

**Precondition:** Room with known actor numbers and four configured spawn points.

1. Start with at least two actors and record sorted actor order.
2. Compare each assigned spawn with its sorted index.
3. Disconnect/rejoin an actor while another client remains active.
4. Exercise an index beyond the fourth spawn in a harness without altering production capacity.

**Expected:** Sorted actor order chooses the assigned point; overflow uses the
first point. A rejoining client has one owned model, not old plus replacement.
Recovery respawns normally; it does not promise restoration of prior position.

### L13 — Owned movement, camera, Q, and pitch

**Precondition:** A and B spawned with working cameras/controllers.

1. Move/look on A while watching B; then reverse clients.
2. Look fully up/down and inspect pitch clamping around +/-85 degrees.
3. Press Q, move the mouse and movement keys, then press Q again.
4. Exercise camera reset after a presentation; repeat near interactable UI.

**Expected:** Only owned avatar processes movement/look; remote camera roots
disabled. Free cursor suppresses local movement/look; relocking and reset remain
usable. Inspect remote instances for unintended cursor interference as well.

### L14 — Remote name tags and fallback identity

**Precondition:** Two visible avatars; synthetic identities.

1. Move/rotate around the remote avatar and observe tag position/orientation.
2. Verify local-owned tag visibility differs from remote visibility as configured.
3. In a fixture, provide PlayerTag; remove it and provide nickname; then remove both.
4. Change the remote PlayerTag and observe update callbacks.

**Expected:** Tag faces the active camera, remains above the model, and follows
trimmed PlayerTag, then nickname, then actor fallback. No duplicate detached tag
objects remain after avatar removal/rejoin.

### L15 — Crosshair target resolution and highlight cleanup

**Precondition:** Test objects with colliders and world interaction components.

1. Aim within and outside configured interaction range; press E.
2. Overlap keypad buttons and other interactable hits; exercise button priority.
3. Move across targets and away, then disable/destroy the highlighted object.
4. Probe an occluded target and record actual raycast-hit selection.

**Expected:** Prompt, chosen action and highlight agree with sorted-hit/priority
logic; configured range is enforced. Dead targets clear without exceptions.
Do not assume every non-interactable obstacle blocks all later hits: verify the
resolver's actual behavior and report unintended through-object interactions.

## 3. Common puzzle test procedures

Apply these to every puzzle/page below where meaningful. The comic and entry
gate use their own navigation/eligibility variants rather than numeric answers.
Use identifiers such as `P06-2-W-field3` to record individual variants.

| Suffix | Detailed procedure | Expected result |
| --- | --- | --- |
| P | Enter the complete correct configuration; submit once; observe before/during/after the configured two-second main-stage delay | Correct feedback, intended page/stage, reward and timer behavior on both clients |
| W | Change exactly one correct field/card/cell; submit; repeat independently for other inputs | Failure without stage/reward; wrong feedback identifies applicable input |
| M | Remove one required input, then try completely blank submission | No false success from missing data; no exception; correcting it permits retry |
| C | After W/M, correct only the failing answer and resubmit | Current input is reevaluated; stale feedback does not block success |
| R | Enter wrong data on each page; let the real deadline expire or invoke the documented reset in an isolated fixture | Current stage resets, required page/controls/card homes restored; earlier progress retained |
| E | Close/reopen before submit, after wrong submit, and during supported page states | Attempt access and shared content remain consistent; closing does not invent a completed stage |
| CO | A owns Submit while B contributes; reverse ownership on another attempt | Shared input remains cooperative; submission UI/authority behave consistently |
| DU | Submit/click repeatedly before and during transition; observe replay/rejoin | No unintended duplicate progression/reward; record existing idempotency gaps |

For card puzzles inspect hidden-panel slots and cards owned by another actor.
For multipart reset distinguish manager-local reset from the full
`PuzzleStateManager` dispatcher: they do not necessarily clear the same objects.

## 4. Opening comic and puzzle-specific cases

### P00 — Nine-slide opening comic

**Precondition:** Fresh stage 0; A/B active; no prior close votes for this round.

1. Navigate forward/back; inspect Close before the last of nine slides.
2. Let A reach the last slide and vote while B remains earlier.
3. Confirm the comic stays open; let B reach the end and vote.
4. Repeat with an inactive peer, and separately let stage 0's timer expire.

**Expected:** Close appears on final slide, remains visible/disabled with waiting
feedback after a vote, and only required active votes finish the gate. Master
advances to stage 1. No analytics score row. Stage 0 timeout currently has no
automatic reset/restart; record it distinctly from stages 1–11.

### P01 — Decode hypoglycemia symptoms

**Precondition:** Stage 1; phone trigger usable; inspect configured slot answer lists.

1. Populate every registered slot with a first card accepted by that slot.
2. Repeat with one empty slot and one wrong first card; inspect feedback.
3. Exercise an additional card and a registered slot under a hidden panel in a fixture.
4. Run reset/re-entry/cooperative variants, then submit the complete correct layout.

**Expected:** All registered slots require a correct first card; serialized
`required` does not control this validator. Correct completion hides phone trigger,
opens Puzzle 2, and preserves timer deadline. An empty slot registry can produce
false success; treat missing registration as a configuration defect, not a valid solve.

### P02 — Align the palette

**Precondition:** Stage 2; configured threshold 10.

1. Place nine accepted first cards and submit.
2. Add the tenth correct placement; repeat with more than ten in a fixture.
3. Add wrong/empty slots and multiple-card arrangements to inspect first-card counting.
4. Complete normally, then test replay/rejoin for reward duplication.

**Expected:** Nine fails; at least ten correct first placements succeeds. Unlike
stage 1, every slot need not be filled. Award Time Crystal1 and clue `325`, enter
stage 3/Canteen, reset timer, and exclude stage 2 from CSV. The debug win message
is not the game ending.

### P03 — Picture grid exact-set selection

**Data:** 20 buttons; correct zero-based indices `0, 1, 7, 8, 17, 19`.

1. Select exactly those six and compare counts on A/B.
2. Omit each correct index independently; add each non-answer independently.
3. Select six including one wrong replacement; deselect/reselect after feedback.
4. Reopen the panel and force a missed callback in a fixture to test reconciliation.

**Expected:** Exact set only; neither equal count with wrong entries nor a correct
subset succeeds. Feedback, selected-count label and Submit visibility reconcile
with shared button state. Correct completion opens Puzzle 4 without timer reset.

### P04-1 — Survival-code card page

**Precondition:** Stage 4 part 1; threshold 20.

1. Submit 19, then 20 correct step-card placements; probe 21 in a fixture.
2. Corrupt one step match, leave a gap, and test an occupied slot.
3. Test padded/mixed-case step metadata in fixtures; include hidden slots/peer-owned cards.
4. Solve part 1, close/reopen, and reset from part 2.

**Expected:** Threshold 20 yields `Advanced`, not main-stage `Completed`. Both
clients see part 2; stage remains 4; no final reward at this point. Full stage reset
returns part 1 and restores card/input state through the dispatcher.

### P04-2 — Survival-code text page

**Data:** First field exactly `15`; second exactly `4`.

1. Submit correct strings and compare feedback.
2. Independently try swapped values, blanks, ` 15`, `4 `, `15.0`, and `04`.
3. Correct a rejected entry and retry; inspect wrong-answer snapshot content.
4. Reset and inspect both fields plus part visibility; then complete again.

**Expected:** Exact string validation; numeric equivalence/whitespace is not
accepted here. Final success grants Crystal2 and clue `154`, enters stage 5/
Physician Room, and resets timer. Snapshot should not misreport already correct
answers; note any difference between snapshot trimming and actual validation.

### P05 — Word-search completion and selection feedback

**Data:** 21 x 21 including headers; `FLUOROQUINOLONES`, `SULFONYLUREAS`,
`LINEZOLID`, `AZOLES`, `ELDERLY`, `IMPAIRED`, `REDUCTION`.

1. Select every target word; compare boards and selection on A/B.
2. Remove one letter/whole word; add a filler cell; alter a letter in a fixture.
3. Drag to add then remove cells; attempt to select coordinate headers.
4. Submit an incomplete word overlapping a complete one; reset/reopen repeatedly.

**Expected:** All seven words and no unrelated selections required. Complete word
cells green; incomplete/outside selections red; red takes overlap precedence.
Headers unplayable. Board filler deterministic for same room/dimensions; reset
does not multiply grids. Grant clue `477`, close panel, enter stage 6, reset timer.

### P06-1 — Glucose Diary page 1

**Data:** `32`; accepted numeric equivalents include padded `32`, `32.0`, `3.2e1`.

1. Submit each equivalent independently from a clean page.
2. Try blank, null in fixture, wrong value, and numeric differences inside/outside
   the configured 0.0001 tolerance; use an unambiguous larger error such as `32.01`.
3. Test invariant decimal parsing against `32,0` and text case-sensitivity fixtures.
4. Submit correctly and compare page state without invoking the next-stage hook locally.

**Expected:** Trimming/case rules and numeric tolerance apply; unrelated text and
out-of-tolerance numbers fail. Result `Advanced`; explicit synchronized page
transition shows page 2, main stage remains 6. Submission alone must not silently
change `currentStage` in the isolated validator.

### P06-2 — Glucose Diary page 2

**Data:** Field order `24, 8, 8, 8`.

1. Fill all four fields correctly and submit.
2. Independently replace each field with wrong and blank values; correct and retry.
3. Mix valid numeric formats while retaining field order.
4. Compare wrong snapshot numbering; reset/re-enter and inspect hidden-page fields.

**Expected:** All configured page-2 answers required. Wrong snapshots omit correct
values and identify the failing question. Correct result advances both clients
to page 3 without a main-stage reward or stage change.

### P06-3 — Glucose Diary page 3 and full reset

**Data:** Field order `40, 20, 40`; eight fields across all pages.

1. Submit correct page 3 after pages 1/2.
2. Repeat wrong/missing variants independently for each field.
3. Before completion, reset from page 3 and inspect every page's stored text/color.
4. Complete all pages again and verify reward and guidance.

**Expected:** Final result `Completed`; Crystal3; stage 7/Pharmacy; timer reset.
Reset clears all eight fields and feedback, activates only page 1, and restores
current diary page 1. No intermediate page may award final completion.

### P07 — Medicine risk dropdown table

**Data:** Six dropdown indices `1, 2, 1, 1, 1, 1`.

1. Set the exact indices and submit; repeat with each dropdown independently wrong.
2. Reset and confirm all return to initial option and neutral color.
3. Inspect actual option labels and indices in GameScene; do not infer from label order.
4. Remove one reference only in a fixture and observe skipped validation.

**Expected:** Correct configured entries succeed; wrong entries prevent completion.
All six scene references must exist because null entries are currently skipped.
Grant clue `28`, open Puzzle 8, reset timer. A null-reference false positive is a
wiring defect, not evidence that the remaining five constitute a valid answer.

### P08 — Prescription dose string

**Data:** `28`, ` 28 `, `028`, `28.0`, empty, and `27`.

1. Submit each value independently.
2. Inspect green/red feedback and correct a failure.
3. Reset; verify empty input and neutral feedback.
4. Complete and inspect stage/reward/timer plus world-trigger state.

**Expected:** Trimmed `28` succeeds; `028` and `28.0` fail string comparison.
Crystal4; stage 9/Nurse Education Room; timer reset. The fourth completion-trigger
reference is unassigned in the baseline; do not claim a trigger disappears through it.

### P09 — Insulin crossword

**Data:** 13 x 13; `lispro`, `NPH`, `glulisine`, `aspart`, `detemir`, `Lantus`,
`regular`, `Toujeo`.

1. Fill every active cell; repeat with uppercase/mixed case.
2. Make each active letter wrong/missing in isolated variants; submit an empty grid.
3. Compare cell feedback and wrong-word snapshot grouping; edit from both clients.
4. Reset/rebuild repeatedly, reopen, and complete while observing hint panels.

**Expected:** Every active letter matches case-insensitively; blocked cells do not
require answers. Reset has no duplicate grid/listeners. Success switches across
to image hint, grants clue `135`, enters stage 10 and resets timer. Inspect whole
panel visibility separately; completion does not explicitly close the whole panel.

### P10-E — Syringe entry requires the dedicated trigger and chest history

**Precondition:** Stage 10; chest never opened in this attempt/session setup.

1. Interact with syringe before opening chest.
2. Try other world puzzle triggers while stage 10 is current.
3. Open the configured chest lid, then try the dedicated syringe trigger.
4. Close the lid and retry to distinguish current angle from `HasBeenOpened`.

**Expected:** Before chest history is satisfied, entry denied with usable feedback.
Other triggers cannot launch this stage. Once chest has been opened, the dedicated
syringe entry can proceed even after reclosing, as the current condition is historical.

### P10-1 — Syringe choice, double submission, and delayed part change

**Data:** Correct dropdown index `1`; wrong/default index `0`.

1. Submit wrong/default selection using the dedicated action; then correct it.
2. Observe correct feedback and the two-second delay before shared part 2.
3. Submit twice rapidly; close, reset, and disconnect in separate runs during delay.
4. Inspect current part, dropdown value, and both panels after waiting past delay.

**Expected:** Wrong stays on part 1; correct ordinarily opens part 2 on both clients.
Desired reset/recovery behavior must cancel or supersede stale work. Known defect:
reset currently leaves the local coroutine able to reopen part 2. See D03.

### P10-2 — Nine insulin sequence matches

**Precondition:** Part 2 active; threshold 9.

1. Submit eight, then nine correct matches; probe ten in an isolated fixture.
2. Test one wrong step, empty placement, occupied target, and peer-owned first drag.
3. Reset from this page and inspect syringe page/default dropdown plus card homes.
4. Solve again and compare stage 11 route and timer.

**Expected:** At least nine accepted matches succeed. Panel closes; stage 11/
`Morturary`; timer resets. Full reset restores part 1; local manager reset and
global card reset must both be observed rather than treating them as equivalent.

### P11-1 — Store five lapse answers

**Precondition:** Stage 11 part 1; slots tagged `Stage1Slot`.

1. Fill four then five first-card placements and submit.
2. Use padded/mixed-case lapse strings; inspect stored normalized slot answers.
3. Substitute other nonempty lapse cards to characterize current self-comparison.
4. Advance explicitly, then reset and inspect page/slot state.

**Expected:** Five accepted placements produce `Advanced`; stored strings are
trimmed/lowercase. No Crystal5/CSV request yet. Current part-1 logic stores each
card's own value and compares against it; it is primarily a filled-slot count,
not proof of an independently predefined clinical answer key.

### P11-2 — Linked lapse matching and final puzzle effects

**Precondition:** Part 1 stored answers; part 2 has linked `Stage2Slot` references.

1. Place four matching linked answers; separately test three, wrong value and missing card.
2. Probe an absent/incorrect link in a fixture; inspect which expected answer is used.
3. Complete using synthetic analytics/no real delivery credentials.
4. Repeatedly submit/rejoin; inspect reward, mover, CSV request and timer.

**Expected:** Four correct linked matches complete; Crystal5, configured trigger
hiding/mover and analytics request occur. Desired effects should not duplicate.
Current implementation stays at stage 11, does not stop the timer or set a terminal
win flag. Ending-comic exit is separate; do not equate this submit with Lobby return.

## 5. Shared attempts, input, drag ownership, and recovery

### N01 — Cooperative contribution versus submission ownership

**Precondition:** A owns the current attempt; B sees the broadcast puzzle panel.

1. Have B edit each supported control type: text, dropdown, picture button and card.
2. Confirm A receives each value/layout and can submit that shared answer.
3. Inspect Submit visibility on B; invoke submission from a harness separately.
4. Repeat after B owns the attempt and after master migration.

**Expected:** Contribution remains cooperative. Managed Submit belongs to the
designated actor. Method-level authority must be evaluated independently: hidden
buttons alone do not establish that a modified client cannot advance state.
Record actor, control ownership, lock owner and actual submitter for each variant.

### N02 — Held-attempt denial, close, and re-entry

**Precondition:** A acquired a puzzle lock; B has not acquired re-entry access.

1. A closes and reopens the same attempt before submitting.
2. B tries to start a separate attempt while A holds the lock.
3. Submit an incorrect answer and repeat the attempts.
4. Exercise a previously granted participant's re-entry, then change stage/owner.

**Expected:** Owner can return; incorrect submission does not automatically release
lock. Unrelated new start denied with temporary feedback. Re-entry is tied to
the appropriate stage/owner; old access must not grant unrelated future access.

### N03 — Fair turns and round rollover

**Precondition:** Two, then three active actors; record `Round` and `PlayedRound`.

1. Let only A take a turn and try taking another before teammates play.
2. Let remaining active actors use their turns, including wrong submissions.
3. Observe master round increment and clearing of local played flags.
4. Repeat with one inactive actor and around master migration.

**Expected:** Remaining active players can progress; inactive actors are excluded.
Round advances according to all-active-player participation, not a disconnected
actor. A single wrong answer must not independently fabricate an unlocked new round.

### N04 — First drag of a card owned by another actor

**Precondition:** A owns card PhotonView; B owns or participates in the puzzle.

1. B begins dragging without any earlier ownership transfer.
2. Observe immediate local movement while transfer is pending.
3. Release over a free valid slot before transfer completes.
4. Wait for ownership callback and EventSystem drop processing; compare A/B.

**Expected:** Local preview is immediate and not overwritten by incoming owner
position. Pending target is retained and accepted after transfer/drop dispatch.
Final parent, anchored position, scale and raycast state converge on both clients.

### N05 — Occupancy race and failed ownership transfer

**Precondition:** N04 setup with a deliberately delayed transfer.

1. B releases toward a free target while transfer is pending.
2. Before acceptance, A places another card into that target.
3. Repeat with transfer denied/failed rather than completed.
4. Retry dragging the rejected card to another free slot.

**Expected:** Final acceptance rechecks occupancy. Occupied or failed-transfer
drop rolls back to its origin and restores interaction; no two cards occupy a
single-card slot and no stranded preview/tag remains. Capture both final parents.

### N06 — All drag behaviors, invalid drops, enlargement, and reset

**Precondition:** Cards representing Default, Activity1, Activity1_2, Activity2,
Activity6 and Activity9 behaviors in their configured panels.

1. For each behavior, drop on valid/free, occupied, invalid background, and alternate panel.
2. Drag from both source grid and previously accepted slot.
3. Right-click enlarge and close; test available front/back helpers.
4. Reset, then repeat with another actor's card and monitor drag-name tag.

**Expected:** Behavior-specific parent, home, scale, image and color restored.
Tag visible to peers only during drag and cleared afterward. Enlargement local.
Repeated operations must not accumulate transient buffered click/UI effects.

### N07 — Stable hierarchy paths and hidden-panel slot membership

**Precondition:** Isolated duplicate sibling names and actual hidden puzzle panels.

1. Encode/decode card destinations with identical sibling names and nested children.
2. Restore layout after disable/enable and rejoin.
3. Open/close affected panels repeatedly and inspect registered drop slots.
4. Reset while some panels/cards are inactive.

**Expected:** Paths resolve the intended same-name sibling and preserve pathRoot/
ViewID contracts. Disable alone does not unregister DropSlot. Document validator
or reset leakage involving hidden/global slots; do not assume collections contain
only the visible puzzle. Never rename scene hierarchy to make the test pass.

### N08 — Lock-owner departure versus unrelated departure

**Precondition:** A holds attempt; B remains active; use C for unrelated departure.

1. Disconnect A before submitting; watch B's panel and room lock.
2. Let B explicitly start a new attempt after repair.
3. Repeat with C leaving while A remains the valid lock owner.
4. Repeat temporary inactive departure and intentional leave separately.

**Expected:** Abandoned owner lock releases and abandoned panel closes; B can
explicitly start rather than being silently assigned ownership. Unrelated departure
must not terminate A's valid attempt. Remaining players cannot be stuck indefinitely.

### N09 — Disconnect during a pending main-stage completion

**Precondition:** Correct main-stage answer ready; normal delay two seconds.

1. Submit and inspect `PendingAdvanceStage` plus `PendingAdvanceAt`.
2. Disconnect submitter before deadline while another active client remains.
3. Separately disconnect current master during the same window.
4. Wait beyond deadline; inspect resulting stage, lock, panels and rewards on survivors.

**Expected:** Current master processes due pending state; survivors reach the
intended next stage with usable lock state. Record duplicate effects/replay races.
This contract is not an atomic exactly-once transaction and is not a complete
CurrentStage room snapshot.

### N10 — Master migration at different coordination states

**Precondition:** At least two active clients; record state before migration.

1. Change master while idle, during a valid held attempt, and after owner departure.
2. Repeat while denial warning is shown and while a round is rolling over.
3. In isolated setup, omit an expected room property to exercise initialization.
4. Continue interaction/submission on the surviving master.

**Expected:** Missing properties initialized, orphan lock repaired, valid ownership
preserved and pending completion processed. Warning clears on unscaled time even
if a clue paused local scaled time. Migration must not strand future progress.

### N11 — Multipart page recovery after disconnect

**Precondition:** Stages 4, 6, 10 and 11, with a peer remaining active.

1. For each page boundary, disconnect just before and just after its transition.
2. Edit an answer on the next page while the first client is absent.
3. Rejoin within actor TTL and reopen the puzzle.
4. Compare active page, answers, feedback, submit owner and timer on both clients.

**Expected acceptance:** Clients converge without reversing completed work or
exposing incompatible pages. **Current limitation:** Multipart RPC/local-delay
paths lack complete durable recovery. Record the exact stage/part and failed
boundary; passing main-stage recovery does not establish substage recovery.

### N12 — Temporary reconnect, stale active actor, and avatar replacement

**Precondition:** A/B in GameScene; at least one client stays connected.

1. Temporarily disconnect A; allow reconnect/rejoin within ten-minute actor TTL.
2. Observe fast reconnect-and-rejoin and fallback reconnect/cached-room paths.
3. Force an active-joiner response while old connection is still active; retry later.
4. Count owned avatars, inspect identity and position, and resume a puzzle.

**Expected:** Cache retained for active-joiner retry; successful return restores one
avatar. Inactive old avatar cleanup does not create duplicate owned models.
Position uses normal spawn selection, not a saved-world-position guarantee.

### N13 — Intentional leave, TTL expiry, empty room, and new-user rejoin

**Precondition:** Separate test runs with recorded room/player lifetimes.

1. Use intentional Leave and inspect local cached-room clearing.
2. Rejoin after actor expiry; separately wait for an empty room to expire.
3. Disconnect the last active player and attempt later recovery.
4. On a fresh identity/process without cache, select a listed running room.

**Expected:** Intentional leave clears cache. Expired recovery fails cleanly and
returns to usable Lobby. New user cannot use returning-actor UI as a fresh join.
Room cache is not a save file; record actual room availability in last-player tests.

### N14 — Replay of shared values, doors, rewards, and story effects

**Precondition:** Collect examples of each shared state before a peer disconnects.

1. Change text/dropdown/selection/card layout and feedback, unlock a door, and complete a reward stage.
2. Rejoin an actor; separately disable/enable affected controls.
3. Observe buffered stage/story effects, audio/popups and final movement.
4. Compare restored values and count repeated rewards/notifications.

**Expected:** Supported durable properties/buffered state restore correctly.
Desired behavior avoids duplicate rewards, stale popups or restarting movement.
Record unsupported recovery separately; current historic replay is not a complete
checkpoint for every UI flag, callback, credential or analytics event.

### N15 — Concurrent inputs, sequence ordering, and callback suppression

**Precondition:** Shared text/dropdown/button controls; isolated event-order harness.

1. Alternate rapid edits from A/B and observe final values on both.
2. Deliver sequence 2, then 1, then duplicate 2 from one actor; deliver sequence 1 from another actor.
3. Deliver out-of-range dropdown indices and selection/result updates.
4. Disable/enable controls and verify remote application does not trigger outgoing echoes.

**Expected:** Per-actor stale/duplicate text ignored; a different actor's first
sequence remains valid. Dropdown values clamp to available options. Callback
suppression is released after application, colors/results preserved, and clients converge.

### N16 — Mixed Photon protocols under live use

**Precondition:** Synthetic analytics and clue/word-search/crossword traffic enabled.

1. Edit crossword letters while non-master analytics sends events to master.
2. Trigger regular-clue remaining-count traffic while word-search events are processed.
3. Inspect event codes, payload shapes, handler exceptions and unrelated state changes.
4. Rejoin and repeat after master migration.

**Expected acceptance:** Distinct allocations and validated payloads prevent
cross-system casts/state corruption. **Known failure:** Event 41 carries analytics
and crossword text; event 42 carries clue counts and word search. D01/D02 confirm
allocation collisions; they do not by themselves execute all malformed live payload paths.

## 6. Timer, pause, and race conditions

### T01 — Initial duration and warning cycle

**Precondition:** Fresh GameScene timer with configured 900-second duration.

1. Observe initial 15:00 and compare deadlines/displays on A/B.
2. Cross 300 seconds then 120 seconds; record each warning/message and animation.
3. Remain below a threshold and confirm it is not repeatedly emitted each frame.
4. Reset timer, then cross both thresholds again.

**Expected:** Five-minute and two-minute warnings occur once per cycle; configured
ten-second banners complete. Reset clears shown flags and visible warning state.
Master emits shared warnings; local timescale must not be mistaken for timer authority.

### T02 — Timeout at every stage and multipart page

**Precondition:** Arrange stage 0–11 and each multipart page in separate runs.

1. Enter wrong/partial data, including a card owned by the other actor.
2. Let the shared deadline reach zero; inspect both clients immediately afterward.
3. Reopen/attempt the reset stage and correct the answer.
4. Confirm earlier-stage rewards/progress survive and master restarts the appropriate timer.

**Expected:** Stages 1–11 retry current stage rather than global game loss. Inputs,
cards, feedback and multipart first page are usable. Stage 0 currently has no
timeout reset/restart handler. Record stale pending transitions as T05 defects.

### T03 — Completion timer exceptions

**Precondition:** Record remaining time before each successful main-stage submission.

1. Complete stage 1→2 and stage 3→4 with a clearly non-initial remaining duration.
2. Compare actual deadline rather than only rounded displayed seconds.
3. Complete other numbered hooks and inspect timer restart.
4. Complete stage 11 and continue observing countdown.

**Expected:** 1→2 and 3→4 preserve deadline. Other configured completion hooks
reset it, including 11. Stage 11 does not stop countdown or set a terminal latch;
do not require a frozen victory timer as current behavior.

### T04 — Clue pause, Q, controls, popup, and autoplay

**Precondition:** Countdown close enough to expiry; clue and autoplay content available.

1. Open a regular clue and verify local `Time.timeScale` is zero.
2. Wait real time while observing countdown; press Q and try world/movement controls.
3. Queue popups and run timed comic content during pause.
4. Close presentation and inspect cursor, movement, camera, animation and timer.

**Expected:** Network countdown continues; unscaled popup/autoplay progresses.
Scaled animation/delays may pause and must recover. Panel visibility is not a
universal input lock, so record actual interaction leakage separately. Isolated
timer test explicitly enables Photon's offline clock without joining a room.

### T05 — Expiry races with completion, votes, and leave

**Precondition:** Repeat with pending main-stage advance, insulin delay and close gate.

1. Set deadline near completion delay and submit immediately before expiry.
2. Reset or disconnect during that delay in separate runs.
3. Repeat final close vote at expiry and with one actor leaving at the same instant.
4. Inspect settled stage/page/lock/timer/rewards after all callbacks arrive.

**Expected acceptance:** One coherent result, no stale page reopening, double
advance, stranded gate/lock or reversed UI. Preserve timestamps and reproduction
order; a nominal successful run does not establish all event orderings. Insulin
reset race is already reproduced by D03.

## 7. Clues, comics, inventory, world, story, and layout

### U01 — Comic navigation, final-slide Close, and fresh vote rounds

**Precondition:** Each ordinary gated comic; A/B active.

1. Navigate first/middle/last pages and attempt further previous/next actions.
2. A votes at last slide before B; inspect visible disabled waiting button.
3. Let B vote; reopen the same comic when supported.
4. Inspect new round numbers and verify old votes cannot immediately complete it.

**Expected:** Navigation is local; Close last-slide only. Gate requires current-round
active votes. Reopening cannot reuse a permanent boolean-style vote. Ending comic
uses a separate local exit behavior tested in U10.

### U02 — Regular clue takes over Close from slideshow

**Precondition:** Slideshow active under shared Clue root; regular clue available.

1. Open regular clue while slideshow is on first, middle and last slide in separate runs.
2. Inspect slideshow activation and its Close listener ownership.
3. Click regular Close locally; deliver remaining-count event/property callbacks.
4. Let peers finish votes and count clue-close callback invocations.

**Expected:** Slideshow deactivated/listener detached; normal Close path restores
visible button. After vote, waiting text remains and button is disabled rather
than hidden. One click cannot invoke both controllers; final close callback occurs once.

### U03 — Close-gate membership changes and schema separation

**Precondition:** Two, then three clients in a current clue/comic gate.

1. A votes; another player leaves before voting, then repeat after that player's vote.
2. Make one actor inactive without intentional leave.
3. Switch master and rejoin an actor while gate is open.
4. Inspect required count, displayed remaining count, round and completion state.

**Expected:** Only active actors block closing; counts reconcile without a stuck
gate. Regular-clue actor-suffixed room vote keys and comic player vote properties
retain their distinct integer-round schemas. Do not replace them with permanent booleans.

### U04 — Locked clues, collection deduplication, unread badge, local detail

**Precondition:** Clue grid with at least one locked item and one collectible image.

1. Click locked slot; inspect explanation and continued clickability.
2. Collect image once, then repeat same collection through a fixture/replay.
3. Inspect revealed icon and badge; open full image and close it on A only.
4. Check B's reading state and A's unread count.

**Expected:** Collection set deduplicates; new clue increments badge once, viewing
clears it. Correct icon/full image shown. Collected-image detail closes locally
without starting a new multiplayer close round or closing B's independent detail.

### U05 — Shared pickup and simultaneous interaction

**Precondition:** B non-master; world key/clue pickup; inventory/UI systems ready.

1. B picks up while A observes; compare inventory/clues and notification.
2. Repeat simultaneous A/B interaction on a fresh pickup.
3. Rejoin after removal and inspect buffered additions/world object state.
4. Use a key on a door and verify it remains in inventory.

**Expected:** Shared deduplicated addition, intended notification and master removal
of pickup. No double inventory entry or persistent duplicate world pickup. Collection
and transient popup replay are separate expectations; keys are not consumed.

### U06 — Crystal/password rewards and replay

**Precondition:** Complete each reward stage using synthetic session data.

1. Record inventory/clue state before and after each completion.
2. Verify crystals after stages 2/4/6/8/11 and password clues after 2/4/5/7/9.
3. Rejoin, replay buffered completion and switch master around reward delivery.
4. Count additions and new-clue popups independently.

**Expected:** Exact stage mapping and no unintended repeated popup/reward. Room
reward markers reduce replay duplication but are not an atomic transaction.
Password mapping: 2→325, 4→154, 5→477, 7→28, 9→135.

### U07 — Sidebar switching, blocker, and local card popup

**Precondition:** Inventory, clues and other configured sidebar panels available.

1. Open one panel, switch directly to another, then toggle the same panel closed.
2. Open again and use Close All; inspect blocker and active popup reference.
3. Enlarge a card locally, close it, and repeat around a puzzle panel.
4. Inspect cursor/focus and world interaction after every close path.

**Expected:** One sidebar popup at a time; blocker matches visible state; no
invisible blocker intercepts input. Card enlargement is local. Camera/cursor
recovery must be verified with actual controls, not assumed from SetActive calls.

### U08 — Stage-driven story toggles, text, audio, and delayed messages

**Precondition:** Record each StorylineDirector directive before executing stages 0–11.

1. At every transition inspect activate/deactivate lists and disabled components.
2. Inspect keep-enabled exceptions and renderer visibility.
3. Compare queue flashes, timeline/status text and enter audio on both clients.
4. Time enter/exit popups; rejoin to test initial-apply and historical replay behavior.

**Expected:** Configured objects/components and local events change as authored.
Queue/text/audio align with stage. Stage-8 exit delay is 38 seconds; other delays
follow their configuration. No unintended renderer disabling or stale historical
audio/popups. Master-broadcast story dates may differ from locally computed labels.

### U09 — Local TriggerComic collider filtering and once-only activation

**Precondition:** Fresh local trigger with configured objects/text.

1. Enter with a non-Player-tagged collider; inspect activation.
2. Enter with local Player collider, then exit/re-enter.
3. Reset setup and let a remote Player collider enter the local trigger.
4. Compare other client's comic state without assuming broadcast.

**Expected current behavior:** Non-player ignored; Player activates once locally
and updates configured text. No ownership filter or direct broadcast exists.
If remote-avatar activation is undesirable, record that product defect instead
of reporting a synchronized comic open based only on one screen.

### U10 — Nurse autoplay and independent ending exit

**Precondition:** Seven-slide nurse dialogue and separate two-slide ending available.

1. Run nurse dialogue at timescale zero; check durations/audio and completion.
2. Disable mid-slide; verify audio and autoplay stop; re-enable and inspect restart.
3. At ending, let A reach final slide and Close while B continues reading.
4. Let B finish later and inspect both Lobby returns.

**Expected:** Timed dialogue uses unscaled playback; disable cleans it up.
Ending does not require all-player votes: A leaves/loads Lobby independently and
B can finish. Puzzle 11 completion alone does not invoke this local exit.

### U11 — Ordered guidance through rooms and floors

**Data:** Stage 3 Canteen/7; stage 5 Physician Room/4; stage 7 Pharmacy/8;
stage 9 Nurse Education Room/6; stage 11 `Morturary`/6.

1. Traverse each route's authored waypoints, including stair segments.
2. Trigger a later waypoint before current one; trigger another route's waypoint.
3. Approach within 1.5 metres; test different floor elevation and camera pitch.
4. Reach final point, then enter a stage with no route.

**Expected:** Only current path/index advances; proximity uses 3D arrival distance.
Arrow remains horizontal and points along authored route, hides at destination
and clears at other stages. Each client progresses locally; no NavMesh guarantee.

### U12 — World keypad denial, retry, grant, and shared unlock

**Data:** Codes `325`, `154`, `477`, `28`, `135`; maximum nine input characters.

1. Enter wrong digits, submit, and wait the display delay before retry.
2. Use backspace on empty/nonempty input; add one digit beyond nine.
3. Enter correct code, wait, then try more input/repeated submit.
4. Observe linked doors/key rewards from peer, then rejoin after grant.

**Expected:** Denied feedback clears for retry; backspace safe; digit-button input
capped; Granted latches and further input ignored. Assigned doors unlock/open as
configured. Check same shared result after rejoin. Keypad result relay is not
server-side secure digit validation.

### U13 — Door locks, keys, hinges, animation, and highlight cleanup

**Precondition:** Test hard-lock, requires-key, left/right and mirrored door variants.

1. Try E while hard-locked, then while missing required key.
2. Supply key or keypad unlock and inspect requirement removal/open-on-unlock.
3. Toggle open/closed with instant and smooth rotation; inspect hinge direction.
4. Disable while highlighted/animating and inspect scale/emission cleanup.

**Expected:** Correct denial/prompt; no consumed key. Rotation matches leaf and
mirroring. `HasBeenOpened` remains true after closing, supporting chest history.
No stuck highlight or wrong-world-position reparenting.

### U14 — Final world movement, repeat invocation, and rejoin

**Precondition:** Configured stage-11 mover; test transport without real email.

1. Trigger movement, invoke again while moving, and again after arrival.
2. Compare interpolated position and reached flags on A/B.
3. Rejoin during motion, then separately after arrival.
4. Repeat with master/ownership changes and record timestamps.

**Expected:** No restart after reached; current peers converge to target.
Mid-motion timestamp replay is not fully durable in the baseline; record drift,
teleport/restart or inconsistent flags as recovery defects rather than assuming
position synchronization reconstructs elapsed motion.

### U15 — Aspect ratios, readability, reachability, and overlays

**Precondition:** Run all five resolutions listed in section 1.3.

1. At each size open home/room forms, chat, sidebar, every puzzle page and image/comic.
2. Inspect longest labels, input text, waiting-player Close text, and scroll bounds.
3. Drag cards to edge slots; click next/previous/Submit/Close at screen edges.
4. Combine timer warning, popup, chat and clue overlays; capture failures.

**Expected:** Required content readable, controls reachable/clickable, no clipping,
overlapping room entries, stretched critical images or invisible blockers.
Record each resolution separately; success at reference size cannot establish
portrait/ultrawide correctness or actual touch input support.

### U16 — Date offsets, formatting, and rollover

**Precondition:** Controlled fixture date/culture inputs; avoid changing OS settings.

1. Exercise day/month/year boundaries and leap-day examples in isolated formatting tests.
2. Check Diary, Prescription and DynamicDateText offsets/formats.
3. Test story date tokens with/without offsets and date-disabled directives.
4. Compare device-local labels with master-broadcast story text on differing test dates.

**Expected:** Offsets and formatting match each component's authored contract.
Do not require all local date labels to equal a master-computed date when clocks
differ. Record unsupported clock injection as Blocked, not a fabricated execution.

### U17 — Auxiliary rotation tiles

**Precondition:** Local rotation-puzzle fixture, not a twelfth registered game stage.

1. Set each supported correct angle: 0/90/180/270 degrees.
2. Rotate four times and compare visual angle with internal rotation state.
3. Check fully correct and one-wrong-tile configurations.
4. Reset repeatedly and check visual/internal correctness again.

**Expected:** Quarter turns wrap and solution checks agree with appearance.
Investigate reset methods that rotate transforms without updating internal
rotation fields. Do not register this as an extra main-stage requirement.

## 8. Photon Chat

### C01 — Connection identity and message submission

**Precondition:** Fresh Lobby; separate Chat service available; synthetic names.

1. Observe Chat before nickname entry; complete home identity setup.
2. Send empty and whitespace-only messages, then padded normal text.
3. Send using button and Enter separately; rapidly repeat once to check duplicate handlers.
4. Compare sender/peer content and inspect final trimmed text.

**Expected:** Chat waits for an identity, rejects blank content and publishes
trimmed text once per intended submission. Connection readiness and PUN room
readiness are separate; record service-specific failures accurately.

### C02 — Channel choice before room, in waiting room, and in GameScene

**Precondition:** A master, B joiner; Chat connected independently on each.

1. Send before joining and inspect Global channel behavior.
2. Join waiting room, inspect dropdown/channel/subscriptions, and exchange messages.
3. Start GameScene and exchange messages again.
4. Repeat after master migration and return to Lobby.

**Expected current behavior:** Outside rooms Global; Lobby master Global while
joiner room channel; GameScene both room channel. Waiting-room conversation can
split. Record it as a known product gap rather than assuming already unified chat.

### C03 — Unread count, collapse, rebind, and repeat sessions

**Precondition:** Chat connected; peer can send uniquely numbered test messages.

1. Hide chat; send several messages from peer and inspect badge increments.
2. Open chat and verify badge/count reset; collapse and send again.
3. Transition Lobby→GameScene→Lobby without restarting process.
4. Repeat in a second room; exercise input, send button, dropdown and collapse control.

**Expected:** Correct unread state and visible messages; persistent manager binds
new scene UI once, with no duplicate listeners or dead references. Collapse does
not leave essential input controls or a blocker unusable.

### C04 — Independent Chat/PUN connection failures

**Precondition:** Isolated service/network test setup with no OS security changes.

1. Interrupt Chat while keeping PUN gameplay connected; attempt normal interactions.
2. Restore Chat service and observe recovery and channel selection.
3. Separately reconnect PUN while monitoring Chat state.
4. Change master, then repeat after scene transition.

**Expected acceptance:** Communication recovers or clearly reports loss without
breaking gameplay. Current independent Chat reconnect is incomplete and private-
message callbacks are empty. Do not label either feature supported based only on
PUN rejoin success; record the exact service that failed/recovered.

## 9. Synthetic analytics, CSV, and direct SMTP

### A01 — Event attribution, stage exclusions, and multipart identity

**Precondition:** Isolated logger fixture; synthetic room and two actor identities.

1. Apply Enter, Answer(correct/wrong), TimeSpent and PlayerInput events for each stage.
2. Tag stage 4/6/10/11 events with actual part; submit untagged versions separately.
3. Submit from two actors and two multipart parts, including negative elapsed time.
4. Inspect individual/team dictionaries and event attribution before file export.

**Expected:** Stage 0/2 reporting excluded through supported logging paths.
Untagged multipart events dropped; crossword forced part 0. Team identity includes
submitter and part; elapsed time nonnegative. Direct private helper invocation
must not be mistaken for complete public event-path exclusion coverage.

### A02 — CSV sections, counts, exclusions, snapshots, and escaping

**Precondition:** Synthetic attempts only; test-owned output or isolated export environment.

1. Prepare wrong attempts followed by success for selected single/multipart puzzles.
2. Generate CSV and parse both `INDIVIDUAL_PLAYER_INPUTS` and `TEAM_SUBMISSIONS`.
3. Check tries/correct/wrong, variable wrong-answer columns and first-success expected answers.
4. Include comma, quote, CR, LF and empty/null synthetic cells; parse rather than splitting on commas.

**Expected:** Valid rectangular sections with expected fields and round-trip cell
content. Individual rows suppressed for 4/part2, 5, 6/parts1–3, 8 and 9. First
successful expected snapshot is retained; later successes do not overwrite it.
Helper escaping tests do not establish the full on-disk export lifecycle.

### A03 — Submitter semantics, duration formatting, and team total

**Precondition:** A/B contribute different inputs; known synthetic event durations.

1. Have B edit while A submits; inspect exported individual and team attribution.
2. Use durations -10, 0, 59, 60, 900 and 3600 seconds in fixtures.
3. Sum team-row durations and inspect aggregate placement.
4. Compare recorded attempt intervals with full session wall time.

**Expected:** Reporting follows submitter snapshots; it is not every teammate's
raw edit history. Negative duration clamps; spreadsheet-text outputs include
`'00:59`, `'01:00`, `'15:00`, `'60:00`. Summed team total appears only first team
row. CSV escaping is not a complete spreadsheet-formula sanitization layer.

### A04 — Export triggers and quit/leave coverage

**Precondition:** Synthetic logger data; direct delivery prevented unless explicitly tested.

1. In separate runs complete puzzle 11, use GameManager leave, and use GameManager quit.
2. Repeat RestartLevel, standalone QuitButton and ordinary OS/window close.
3. Record whether export was requested and completed before process/room shutdown.
4. Observe non-master behavior while in room and after leaving.

**Expected:** Supported call paths export according to master/eligibility guards.
Record gaps for alternative exit paths and interrupted async work. There is no
universal OS-close flush or guaranteed awaited email shutdown. Do not treat a
file existing from a prior run as evidence the latest exit exported successfully.

### A05 — Export failure/retry and second-session isolation

**Precondition:** Synthetic test environment with controlled file-I/O failure injection.

1. Cause the first export write to fail without modifying real user files/permissions.
2. Remove injected failure and request export again.
3. Start a second room in the same process with distinguishable synthetic data.
4. Inspect write flag, session date, dictionaries and files for both sessions.

**Expected acceptance:** Retry possible and second session isolated.
**Current gap:** `hasWrittenCsv` is set before I/O; persistent logger state/date
and dictionaries lack complete new-room reset. Record suppressed retries, mixed
data or missing second output as failures; preserve synthetic evidence.

### A06 — Historic analytics and organiser eligibility after master migration

**Precondition:** A organiser/master has earlier synthetic events; B active.

1. Migrate master to B after several attempts, then generate more events.
2. Request export while A remains present; separately repeat after A leaves.
3. Compare historical rows available on old/new master.
4. Inspect organiser actor property and local credential availability without logging values.

**Expected current limitation:** Master-local history and credentials do not
automatically migrate. SMTP requires eligible current writer also be organiser
still in room with local credentials. Missing historical rows are a durability
gap, not a valid claim of complete analytics after migration.

### A07 — Verified direct-SMTP test delivery and failure behavior

**Precondition:** Explicitly verified test sender/recipient; runtime secrets only;
synthetic CSV; dedicated test delivery environment. Otherwise mark Blocked.

1. Complete an eligible synthetic export while organiser/master remains in room.
2. Verify receipt and attachment using the configured test mailbox.
3. Repeat SMTP rejection/unavailable-server injection and interrupted quit separately.
4. Inspect sanitized result/error messages and retry/shutdown behavior.

**Expected:** SMTP over TLS, correct synthetic attachment and truthful success/
failure. No password or CSV contents in logs/evidence. Existing success logging
includes recipient address and requires remediation; do not copy it into reports.
No durable retry queue or guaranteed completion before quit exists.

## 10. Optional ASP.NET Core email API

These cases test a separate service. They do not establish a Unity HTTP caller.
Never run valid-delivery cases against source recipient constants or an unverified
endpoint. Use secure test configuration and a verified test recipient first.

### B01 — Health, rejected upload matrix, and rate limiting

**Precondition:** Dedicated known test API; TLS valid, or explicit loopback HTTP.

1. GET `/health`; verify HTTP 200 and JSON `status: ok` (not SMTP connectivity).
2. POST JSON instead of form; then missing `csv`, empty CSV, wrong extension and
   5 MiB + 1 byte synthetic attachment independently.
3. Run `Tests/Test-EmailApiValidation.ps1` against that verified base URI.
4. On an isolated instance/fresh window, add `-IncludeRateLimit`; send invalid
   requests through allowance and observe rejection without submitting valid mail.

**Expected:** Invalid cases 400, limiter 429 after 20 permits/minute with queue 0.
No per-user/IP partition promised. HTTP redirects are observed rather than
bypassed. Fixed-window boundary rollover can affect counts; record request times.

### B02 — Valid boundaries, extension case, filename, and metadata

**Precondition:** Secure test SMTP configuration and verified recipient; B01 passed.

1. Submit nonempty synthetic `.csv` files of one byte and exactly 5242880 bytes.
2. Repeat uppercase `.CSV`, filename containing a path, and ordinary filename.
3. Send blank/unusual `roomName` and `playedAt`; use synthetic metadata only.
4. Confirm attachment name and server response timing against SMTP completion.

**Expected current contract:** Nonempty CSV suffix accepted up to 5 MiB,
case-insensitively; base attachment filename; 204 only after awaited SMTP.
CSV schema/content and date format are not validated. No request recipient field,
authentication, database or retry queue should be inferred.

### B03 — Startup validation, parse failures, SMTP errors and cancellation

**Precondition:** Dedicated API with failure injection and test-owned configuration.

1. Start with invalid required host/port settings; record validation behavior.
2. Submit malformed multipart content and inspect exception/HTTP behavior.
3. Make SMTP reject or remain unavailable; test client cancellation separately.
4. Probe deployment HTTP→HTTPS redirection without disabling certificate checks.

**Expected:** Options validation and configured exception path apply; no false 204
before send completes. Observe whether cancellation actually reaches SMTP; do not
assume a cancelled request cancels `SendMailAsync`. File size check follows form
parsing and is not a custom total-body limit; distinguish proxy/parser rejection.

### B04 — SmtpEmailSettings validation versus actual delivery route

**Precondition:** Isolated settings object; no real mail invocation.

1. Test HTTPS URL, Editor loopback HTTP, remote HTTP, FTP, relative/empty URL.
2. Repeat with `enableSending` false and inspect configuration reason.
3. Trace actual logger delivery call path and change settings endpoint in fixture.
4. Compare behavior without assuming the settings asset is used by the logger.

**Expected:** Valid HTTPS and Editor loopback HTTP accepted; insecure remote or
invalid URL rejected; disabled reports disabled. Active logger remains direct SMTP;
asset's enable flag does not disable that route. No automatic Unity HTTP upload
is implemented merely because settings validation succeeds.

## 11. Scene and Inspector contracts

These inspect actual serialized Lobby/GameScene through preview scenes. Run the
named Edit Mode tests; do not save preview scenes or rewrite YAML to satisfy them.
Historical automated pass applies to the 30 October snapshot only.

### S01 — Enabled scene journey

**Steps:** Inspect enabled build scenes and run `EnabledBuildScenesMatchJourney`.
Check disabled SampleScene/recovery assets are not counted as game stages.
**Expected:** Enabled order Lobby then GameScene; ending returns locally to Lobby.

### S02 — Eleven stage handlers and panels

**Steps:** For each stage 1–11 run `EveryStageHasPanelAndPuzzleHandler`. Inspect
state-manager panel and corresponding manager reference, including inactive panels.
**Expected:** Assigned panel, valid manager implementing IPuzzle, correct stage
mapping. A similarly named activity folder does not substitute for correct reference.

### S03 — Timer, warnings, and main-stage delay

**Steps:** Run `CountdownAndTransitionDelayMatchRequirements`; inspect serialized
timer duration, warning array, timer text and state-manager delay.
**Expected:** 900 seconds, thresholds 300/120, usable timer text, two-second delay.
This checks values/wiring, not live warning transport or animation.

### S04 — Card thresholds

**Steps:** Run `CardThresholdsMatchConfiguredRules` for all parameter rows and
compare manager fields with PRD.
**Expected:** Palette 10; survival 20; insulin 9; lapses 5 then 4. Stage-1
all-slots validation must not be reinterpreted as its unused `required` field.

### S05 — Picture button uniqueness and answer set

**Steps:** Run `PictureGridHasTwentyUniqueButtonsAndExactAnswerIndices`; inspect
duplicate/null assignments and answer-index bounds.
**Expected:** Twenty unique non-null buttons; exactly 0/1/7/8/17/19 as answers.

### S06 — Diary pages and answer references

**Steps:** Run `DiaryHasAllEightFieldsAndThreePages`; inspect stage numbers,
field references, accepted-value arrays and order.
**Expected:** Three pages and eight fields; 32 / 24,8,8,8 / 40,20,40.
No missing field may silently reduce the intended validation requirement.

### S07 — Risk table completeness

**Steps:** Run `RiskTableHasAllSixDropdownReferencesAndExpectedIndices`; inspect
options and each reference.
**Expected:** Six non-null dropdowns with 1/2/1/1/1/1. This prevents skipped-null
validation from appearing to be a correct complete table.

### S08 — Generated-grid dimensions and prefabs

**Steps:** Run `GeneratedPuzzlesHaveCorrectDimensionsAndCellPrefabs`; inspect
assigned CrosswordCell/LetterCell prefab components and grid parents.
**Expected:** Crossword 13; word search 21 including headers; valid cell prefabs.
Live rebuild/listener duplication still requires runtime cases P05/P09.

### S09 — Syringe/chest entry wiring

**Steps:** Run `SyringeEntryHasBothChestAndDedicatedTrigger`; inspect the actual
trigger/lid references and correct syringe index.
**Expected:** Both references assigned to intended objects; dropdown answer index 1.
Reference presence alone does not establish world collider reachability.

### S10 — Guidance route configuration

**Steps:** Run `GuidanceHasExactNamedRoutesAndNoMissingWaypoints`; inspect each
route's unique ID, count and non-null transforms.
**Expected:** Canteen 7, Physician Room 4, Pharmacy 8, Nurse Education Room 6,
`Morturary` 6; exact serialized spelling retained.

### S11 — Keypad combinations

**Steps:** Run `KeypadCombinationsMatchClueRewards` against GameScene keypads;
compare with password reward order and assigned door targets.
**Expected:** 325/154/477/28/135. Standalone prefab defaults are not the scene oracle.

### S12 — Opening versus ending comic configuration

**Steps:** Run `EndingExitsLocallyAndOpeningAdvancesToFirstPuzzle`; inspect slide
counts, opening target and ending leave/gate flags.
**Expected:** Opening nine slides targets stage 1; ending two slides leaves to
Lobby with all-player gate disabled. Runtime independence still requires U10.

### S13 — Shared physical Close reference

**Steps:** Run `ComicsUseRegularClueCloseButton`; inspect all slideshow Close
references against ClueUI's button under Clue root.
**Expected:** Same intended button reference. Correct listener handoff and waiting
text are separate runtime requirements U02/U03.

### S14 — Avatar order and spawn assignments

**Steps:** Run `AvatarsAndSpawnPointsAreAssignedInRoleOrder`; inspect prefab names,
Player tag, PlayerController, CharacterController and four spawn transforms.
**Expected:** Doctor/Nurse/Therapist in order; required player components and spawn
count. Actual remote ownership/camera behavior remains L12–L14.

### S15 — Missing MonoBehaviour scripts

**Steps:** Run `NoMissingMonoBehaviourScriptsInEnabledScenes`, including inactive
children; report exact scene/object for any missing script.
**Expected:** No missing MonoBehaviour components in enabled scenes. Do not delete
components or regenerate references merely to hide the failure.

### S16 — Room-list layout

**Steps:** Run `RoomListHasLayoutToPreventOverlappingEntries`; inspect Lobby
JoinRoomManager contentPanel layout and manually populate at least three entries.
**Expected:** Layout can position clones separately. Historical scene contract
passed; interactive refresh/scroll/resizing remains L06, not automatically passed.

## 12. Confirmed defect diagnostics

Run via **SGH > Tests > Run known-gap Edit Mode diagnostics** or the corresponding
Play Mode command. Failures below were reproduced on 30 October 2026. Rerun to
establish current behavior after fixes; do not weaken assertions to obtain green.

### D01 — Analytics/crossword event 41 collision

**Procedure:** Run `AnalyticsAndCrosswordMustUseDifferentEventCodes`; compare
GameplayLogEventCode with CrosswordTextSyncEventCode.
**Expected invariant:** Different allocations for unrelated protocols.
**Historical actual:** Both 41; assertion failed. Live payload compatibility still
needs N16. A fix must migrate all producers/consumers together and validate types.

### D02 — Clue/word-search event 42 collision

**Procedure:** Run `ClueVotesAndWordSearchMustUseDifferentEventCodes`; compare
ClueCloseRemainingEventCode and WordSearchCellStateEventCode.
**Expected invariant:** Distinct allocations.
**Historical actual:** Both 42; assertion failed. Do not allocate additional traffic
to 41–43 or claim three-field payloads are safe merely because lengths match.

### D03 — Insulin reset during delayed part advance

**Procedure:** In isolated Play Mode, correct dropdown index 1, submit, immediately
reset, then wait over two seconds. Run `InsulinResetDuringDelayMustNotReopenPartTwo`.
**Expected invariant:** Reset keeps part 1 active and part 2 hidden.
**Historical actual:** Part 2 reopened; assertion failed. Test disconnect/master
variants separately; this local reproduction does not establish durable recovery.

## 13. Running tests and recording a result

### 13.1 Automated entry points

- In idle Unity Editor: **SGH > Tests > Run Edit Mode suite**, then **Run Play Mode suite**.
- Use the separate known-gap commands for D01–D03; normal menu runs exclude `KnownGap`.
- Alternative: Window > General > Test Runner. Explicit assembly selection can
  include diagnostic cases, so record the filter used.
- Batch with this project's editor closed: `./Tests/Run-UnityTests.ps1`; use
  `-Mode EditMode`, `-Mode PlayMode`, or `-KnownGaps` as needed.
- Builds compile but do not execute tests: `dotnet build Assembly-CSharp.csproj --no-restore`.
- XML output: `TestResults/sgh-EditMode.xml`, `sgh-PlayMode.xml` and corresponding
  `-diagnostics.xml` files. Outputs are ignored; preserve appropriate sanitized evidence.

### 13.2 Per-case result template

Copy one record for each case/variant, including each resolution and ownership permutation.

```text
Case / variant ID:
Build / commit / Unity version:
Execution date / tester:
Mode: Edit fixture / isolated Play fixture / two-client / visual / delivery
Clients, actor numbers, master, attempt owner:
Scene, stage, part, room (synthetic):
Resolution / latency / disconnect type:
Preconditions and test data:
Steps actually performed:
Expected result:
Actual result:
Status: Pass / Fail / Not run / Blocked
Evidence location (sanitized):
Defect ID, reproduction frequency, recovery/workaround:
Cleanup completed:
```

After each run, close test-only panels, restore timescale/cursor, and leave/reset
the isolated room as appropriate. Do not clear production PlayerPrefs, real
exports, or unrelated user changes. Keep historical results distinct from reruns.

### 13.3 Coverage limits

Passing offline helper tests establishes the invoked behavior under those inputs.
It does not prove two-client delivery, every network ordering, every hierarchy,
all devices, complete recovery, clinical correctness, or durable analytics/mail.
The historical 171 regular passes and three diagnostic failures remain separate
from every manual case's Not-run status. This document adds procedures; it does
not report a newly completed gameplay or email acceptance run.

## 14. Automated test traceability and exact fixture coverage

Parameterized methods expand into multiple NUnit cases. The historical 163/8
counts are case counts, not the number of methods or manual IDs. The tables below
describe actual fixture scope; broader boundary/race variants above remain manual
unless the stated method explicitly exercises them. Setup/teardown and runner
callbacks are infrastructure, not additional acceptance tests.

### 14.1 Answer rules — Edit Mode

Source: [AnswerRuleTests.cs](Assets/Scripts/Tests/EditMode/AnswerRuleTests.cs).

| Automated method | Inputs/procedure and assertion | Manual case |
| --- | --- | --- |
| `DiaryUsesDocumentedComparison` | Against expected 32: exact, padded, 32.0, 3.2e1 and 32.00001 accepted; 32.01, blank, null and 32,0 rejected. `abc`/`ABC` accepted when insensitive, rejected when sensitive. Uses tolerance 0.0001. | P06-1 |
| `SurvivalCodeRequiresExactStrings` | Correct 15/4; leading/trailing spaces, 15.0, either blank and swapped values rejected. Reset empties both inputs. Does not include every manual numeric alternative such as 04. | P04-2 |
| `PrescriptionTrimsButDoesNotCoerceNumbers` | 28 and padded 28 succeed; 28.0, 028, blank and 27 fail. Checks red/green color and reset to empty/white. | P08 |
| `RiskTableRequiresEveryConfiguredAnswer` | All six correct, then each independently wrong at index 0. Checks result and reset of each value/color. | P07 |
| `DiaryAdvancesOnlyThroughExplicitPageChangeAndResetsAllPages` | Configures all eight correct answers/three panels; checks Advanced/Advanced/Completed, explicit page visibility, no page mutation merely from submission, and clearing every field on reset. | P06-1–3 |
| `DiaryWrongAnswerDoesNotAdvanceAndSnapshotOmitsCorrectAnswers` | Page with correct 32 plus wrong 99; result Incorrect; stays page 1; wrong snapshot contains only Question 2 = 99. | P06-1/2 |
| `PictureGridRejectsEveryOmissionAndExtraSelection` | Exact six succeeds; each correct omission independently fails; one representative extra index 2 fails. Reset leaves unsolved and buttons interactable. All other wrong-index combinations remain manual variants. | P03 |

### 14.2 Grids and local interactions — Edit Mode

Source: [GridAndInteractionTests.cs](Assets/Scripts/Tests/EditMode/GridAndInteractionTests.cs).

| Automated method | Inputs/procedure and assertion | Manual case |
| --- | --- | --- |
| `CrosswordChecksEveryActiveLetter` | Builds configured solution cells; correct and uppercase boards pass; one representative missing letter, wrong letter and empty board fail; incorrect snapshot includes expected-word information. It does not parameterize every cell individually. | P09 |
| `WordSearchRequiresAllSevenWordsWithoutExtras` | Finds target-word positions from solution; selects union. Complete set passes; representative missing cell, extra filler at (2,2), wrong letter and empty set fail. | P05 |
| `WordSearchDragAddsThenRemovesAndIgnoresHeaders` | Begin on header ignored; drag across two cells selects both; a removal drag clears both. | P05 |
| `WordSearchFillerIsDeterministicForTheSameSceneAndDimensions` | Clears one filler cell, fills twice, compares same result; checks one fixed target letter preserved. Does not simulate separate room transports. | P05 |
| `CardHierarchyPathsDisambiguateDuplicateSiblingNames` | Round-trips two same-name siblings, nested child and root; second sibling encodes index; missing path returns null. | N07 |
| `KeypadLimitsNineDigitsBackspacesAndIgnoresInputAfterGrant` | Sends twelve single-digit inputs, checks nine stored; backspace removes one; empty backspace safe; latched grant ignores further digits. Multi-character malformed button values are outside this fixture. | U12 |
| `KeypadDenialClearsForRetryButGrantLatches` | Advances result enumerators directly through denial and grant; checks displaying flag, denied clear and Granted text/latch. Does not wait actual display duration or use RPC delivery. | U12 |
| `GuidanceRejectsOutOfOrderArrivalAndCompletesInSequence` | Synthetic three-point Canteen path; later/wrong-route arrivals ignored; ordered arrivals advance; final target null and IsActive false. Real stairs/arrow positioning remain manual. | U11 |
| `InventoryAndCluesDeduplicateAndCluesCanReset` | Same key twice remains one entry; missing key false; blank clue rejected; duplicate clue false; clear removes collection. Does not exercise world pickup transport. | U04/U05 |

### 14.3 Session configuration and analytics — Edit Mode

Source: [SessionAndAnalyticsTests.cs](Assets/Scripts/Tests/EditMode/SessionAndAnalyticsTests.cs).

| Automated method | Inputs/procedure and assertion | Manual case |
| --- | --- | --- |
| `EmailValidationRejectsInvalidAndDisplayNameForms` | Null, empty, whitespace, malformed and display-name addresses rejected for sender/recipient; padded plain synthetic address accepted; reason null only on success. | L02 |
| `CredentialsRequireAllThreeValuesNormalizePasswordAndClear` | Missing values fail retrieval; all synthetic values succeed, sender trimmed/password spaces removed; Clear makes retrieval fail and all returned values null. | L03 |
| `EmptyAppPasswordIsRejected` | Null, empty and whitespace-only synthetic passwords rejected. | L03 |
| `BackendSettingsRequireHttpsExceptEditorLoopback` | HTTPS and HTTP localhost/127.0.0.1 accepted in Editor; remote HTTP, FTP, relative and empty URLs rejected; disabled setting rejected with reason. Does not test actual upload. | B04 |
| `IndividualAnalyticsExclusionsArePreserved` | Suppressed: 4p2, 5, 6p1–3, 8, 9. Not suppressed: 1, 3, 4p1, 7, 10p1/p2, 11p1/p2. | A01/A02 |
| `CsvEscapesSyntheticCells` | Null becomes empty; plain string unchanged; commas, embedded quotes, LF and CR quoted/escaped. No formula sanitization assertion. | A02 |
| `CsvDurationIsNonnegativeSpreadsheetText` | -10/0→'00:00; 59→'00:59; 60→'01:00; 900→'15:00; 3600→'60:00. | A03 |
| `AnalyticsDropsUntaggedMultipartEvents` | Untagged answer at stages 4/6/10/11 creates neither team nor individual rows. | A01 |
| `AnalyticsExcludesPaletteAndKeepsFirstSuccessfulSnapshot` | Stage 2 answer creates no team row; two stage-8 successes count twice but expected answer remains first snapshot 28. | A01/A02 |
| `AnalyticsSeparatesSubmittersAndPartsAndClampsNegativeTime` | Two actors x two stage-10 parts create four team rows; each wrong count one; negative elapsed becomes zero. | A01/A03 |
| `CreatedRoomsKeepRejoinLifetimeAndLobbyMetadataWithoutCredentials` | RoomOptions for capacity 4/password ABC234: player TTL 600000, empty TTL 300000, user ID publication, exactly password/GameStarted metadata. Does not join cloud. | L05 |
| `GeneratedRoomPasswordsUseConfiguredAlphabetAndLength` | Samples 100 generated values; every length six, every character in configured alphabet; restores Unity random state. Does not assert uniqueness/security strength. | L05 |

### 14.4 Shared controls and local world logic — Edit Mode

Source: [SharedControlAndWorldTests.cs](Assets/Scripts/Tests/EditMode/SharedControlAndWorldTests.cs).

| Automated method | Inputs/procedure and assertion | Manual case |
| --- | --- | --- |
| `InputReplicationRejectsDuplicateAndOutOfOrderSequencesPerActor` | Actor1 seq2 accepted, seq1/duplicate2 ignored; actor2 seq1 accepted. Remote set leaves localSeq zero and suppression false afterward. RPC method invoked directly, no Photon transport. | N15 |
| `DropdownReplicationClampsAndSuppressesLocalCallbacks` | Incoming -10→0, 1→1 and 99→2 on three options; callback suppression released. | N15 |
| `SharedColorsRoundTripAndRejectMalformedPayloads` | RGBA round-trip for DraggableCard/input/dropdown helpers. Reject null, wrong string, two-float array, and int array instead of floats. Does not test every arbitrary network payload. | N15 |
| `WordSearchSelectionAndResultEncodeIndependently` | All six combinations of selected true/false with result None/Correct/Wrong round-trip through packing. | P05/N15 |
| `InactivePlayersDoNotBlockCloseVotesOrTurns` | Null excluded, active included, inactive excluded in ClueUI, SlideshowUI and puzzle-turn eligibility helpers. Does not run complete multi-client voting loop. | N03/U03 |
| `DoorHardLockAndMissingKeyDenyUntilKeypadUnlock` | Locked and missing-key attempts denied; local keypad unlock clears requirements/opens assigned hinge; close retains HasBeenOpened. No live shared door RPC. | U13/P10-E |
| `ComicTriggerRequiresPlayerAndActivatesOnlyOnce` | Non-player collider ignored; Player activates; repeated entry does not activate again. Remote ownership filtering not supplied by this script. | U09 |
| `CharacterSelectionHighlightsExactlyOneRole` | Each index 0/1/2 produces exactly its selected-color button with others normal. It tests highlighting, not network property delivery/spawning. | L10 |
| `SidebarSwitchesPanelsAndBlockerAndClosesAll` | Open A, switch B, toggle B closed, reopen A, CloseAll; checks panel/blocker flags. | U07 |

### 14.5 Scene contracts — Edit Mode

All sixteen contracts S01–S16 map to their named methods in
[SceneContractTests.cs](Assets/Scripts/Tests/EditMode/SceneContractTests.cs).
`EveryStageHasPanelAndPuzzleHandler` expands into eleven stage cases;
`CardThresholdsMatchConfiguredRules` expands into five threshold cases. Other
scene checks inspect their configured collections within one test invocation.

### 14.6 Runtime fixtures — Play Mode

Sources: [CardPuzzleTests.cs](Assets/Scripts/Tests/PlayMode/CardPuzzleTests.cs) and
[PresentationAndTimerTests.cs](Assets/Scripts/Tests/PlayMode/PresentationAndTimerTests.cs).

| Automated method | Fixture sequence and assertion | Manual case |
| --- | --- | --- |
| `SymptomsRequireEveryRegisteredSlotAndRejectWrongOrMissingCard` | Two registered symptom slots; missing fails, wrong fails, corrected succeeds. | P01 |
| `PaletteThresholdCountsOnlyFirstCorrectCardPerSlot` | Nine correct fails, tenth succeeds, additional empty slot still succeeds. Despite name, does not construct every multiple-card ordering; those remain manual. | P02 |
| `SurvivalCardsRequireTwentyAndPartTwoDoesNotCompleteFromPartOne` | Nineteen fails, twentieth yields Advanced; part remains1 until explicit switch; 15/4 then Completed. | P04-1/2 |
| `InsulinRequiresNineCorrectCardsAndResetRestoresSyringePage` | Eight fails, ninth succeeds; explicit part2 then reset returns part1. Does not include a pending part1 coroutine; D03 tests that separate race. | P10-2 |
| `LapsesStoreFiveAnswersThenRequireFourLinkedMatches` | Five stage1 cards store normalized answers and yield Advanced; empty part2 fails, four linked matches complete; reset returns manager stage1. | P11-1/2 |
| `PopupQueueRunsInOrderWhileScaledTimeIsPaused` | At timescale0, First then Second messages display FIFO and queue drains/panel hides using unscaled timing. | T04/U08 |
| `RegularClueTakesSharedCloseAfterSlideshowAndInvokesCallbackOnce` | Two-slide comic starts with hidden Close; regular clue disables comic/restores Close; pauses time; repeated close invocation runs callback once and restores time. Does not establish team vote UI. | U02 |
| `PhotonDeadlineCountdownContinuesWhileScaledTimeIsPaused` | Enables explicit Photon offline clock, sets deadline, timescale0, invokes timer Update across real frames; verifies stopped-at-expiry and 00:00. Restores offline mode afterward. | T04 |

### 14.7 Diagnostics and non-Unity scripts

`ProtocolDiagnosticTests.cs` contains D01/D02. PresentationAndTimerTests contains
D03 as explicit KnownGap. Their historical three failures are retained separately.

`Tests/Run-UnityTests.ps1` orchestrates the same assemblies in batch, checks project
lock, writes XML/logs and returns failure for failing runs; batch launching was not
historically exercised while the project was open. `Tests/Test-EmailApiValidation.ps1`
contains B01 rejected synthetic requests; syntax was checked, but no deployment
was contacted. These are authored scripts, not additional completed acceptance runs.
