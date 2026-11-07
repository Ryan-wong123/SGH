# SGH executable regression scenarios

Baseline: PRD/architecture dated 8 September 2026; suite authored 30 October 2026.
Each row is a test script: establish its precondition, perform its actions in
order, and compare both clients with the stated oracle. Record build/commit,
actor numbers, master, stage/part, actual result and evidence. `Not run` is the
initial status for every manual case. A documented limitation is not a pass for
the desired recovery invariant. Avoid storing credentials or gameplay CSVs in
test evidence.

## Setup and execution dimensions

- A and B are independent processes/device identities in the same isolated test
  room. Use Editor + development build or two builds. Never duplicate a cached
  Photon user ID. Add C for turn/vote and overflow cases where specified.
- Run the automated Edit Mode and Play Mode suites first. Fixtures test real
  methods offline; they do not establish room delivery or GameScene rendering.
- For each shared test, run once with A master and once after B becomes master.
  Test 2 players and configured maximum capacity. Record normal and induced
  latency/disconnect cases separately.
- Use invented usernames and `.invalid` addresses for offline validation.
  Actual test mail requires an explicitly verified test sender/recipient and
  separate secure test configuration. Do not complete Puzzle 11 with real runtime
  mail credentials merely to test gameplay.
- For temporary loss, disconnect the process/network, leaving the actor inactive;
  for intentional leave, use the application's leave action. They have different
  rejoin/cache expectations. Do not change OS security/privacy settings.
- Visual dimensions: 1920×1080 (16:9), 1920×1200 (16:10), 3440×1440
  (ultrawide), 768×1024 (tablet portrait), 1280×720 (mobile landscape). Repeat
  affected UI cases at each size. These dimensions test layout, not touch support.

## Home, rooms, loading, identity and player controls

| ID | Precondition / actions | Expected result |
| --- | --- | --- |
| L01 | Fresh Lobby; submit empty/whitespace username, then a padded invented name. | Blank rejected; trimmed name proceeds to sender entry. |
| L02 | Submit invalid sender, display-name address, valid synthetic address; repeat for recipient. | Invalid forms rejected; each successful step advances exactly once. |
| L03 | Reach app-password step; enter blank then a synthetic spaced value; navigate to recipient. | Password masked; blank rejected; spaces removed in memory; recipient field unmasked. No credential appears in Photon properties/logs/assets. |
| L04 | Finish form, return to Lobby in same process, start another session. | Fresh form state and valid cursor/focus; record persistent-manager/session reset defects rather than assuming a fresh process. |
| L05 | Create empty room name; duplicate cached room name; then a unique name with each capacity. | Validation leaves usable form; successful room has selected capacity, six-character password, 600000 ms player TTL and 300000 ms empty TTL. |
| L06 | List at least three rooms; resize list, refresh repeatedly; remove/full/hidden room while selected. | Entries do not overlap, remain individually selectable, disappear/update correctly, and stale selection clears. |
| L07 | Select waiting room; wrong, padded-correct, empty passwords; room disappears during confirmation. | Wrong denied; trimmed correct joins; missing/stale room handled without frozen UI. Convenience gate is not secure authentication. |
| L08 | A creates, B joins; inspect waiting name/count/password; B tries Start, then A starts. | Only master Start visible/effective; both load GameScene; room remains open/visible with GameStarted true. No all-ready prerequisite. |
| L09 | Fail creation once and allow retry; trigger random-join fallback in a test harness. | Retry uses cached name/capacity/password; fallback has rejoin TTLs and records creator after actor assignment. No competing connection attempts. |
| L10 | Select Doctor=0, Nurse=1, Therapist=2; select same role on both clients; start with missing/invalid selection. | Correct role/highlight; duplicate roles allowed; invalid selection defaults to Doctor. |
| L11 | Observe loading on both clients under fast/slow load; repeat session. | Loading panel/tip/progress stay usable and disappear after load; record existing fade-alpha limitation. No duplicate callbacks/errors after return. |
| L12 | Join with actors ordered differently from connection order, then rejoin; test more than four spawn assignments in harness. | Sorted actor order selects four assigned spawns; overflow uses first; one owned avatar per active actor. |
| L13 | Move/look as A and B; Q unlock/relock; reset camera; inspect ±85° pitch. | Only owned movement/camera; remote camera disabled; Q suppresses local movement/look while free. No remote camera/input interference. |
| L14 | Inspect remote names while moving/rotating and after nickname/property fallback. | Remote tags face active camera; owned tag hidden; PlayerTag→nickname→actor fallback works. |
| L15 | Point E at nearby/far/occluded targets and overlapping keypad button/interactable hits. | Three-metre configured ray range, sorted targets, prompt/highlight matches chosen object; button priority behaves consistently; destroyed target clears safely. |

## Every puzzle and every part

For **each row/part** below execute five cases: **P** correct answer, **W** one
wrong answer, **M** one missing answer, **R** timeout/reset after a wrong answer,
and **E** close/re-enter the same attempt. Run P/W/M using B's controls while A
owns Submit; then swap the attempt owner. W/M must not award rewards or advance.
R must restore the first page, usable controls/card homes and feedback as
implemented; prior completed stages/rewards remain. Capture both clients before
submission, during the two-second delay and after transition.

| Stage / part | Exact input and special variations | Correct-result oracle |
| --- | --- | --- |
| 0 opening | Nine slides; A reaches end/votes before B; B votes last. | Close only on final slide; gate excludes inactive actors; stage 1 only after active votes. No analytics row. |
| 1 | Fill every registered slot with an accepted symptom; include a slot in a hidden panel, then a wrong first card. `required` is not this validator's threshold. | All registered slots must have a correct first card; phone trigger hides; Puzzle2 opens; timer continues. Empty registry false-positive is a configuration risk. |
| 2 | 9, 10 and more than 10 correct first-card placements; wrong/extra/empty slot variants. | At least 10 succeeds; Time Crystal1 and clue 325 once; stage3/Canteen; timer reset. No CSV row. |
| 3 | Exactly indices 0,1,7,8,17,19 of 20; omit each independently; add each wrong index independently; deselect after feedback. | Exact set succeeds; Puzzle4 opens; timer continues. Selected count/Submit visibility reconcile after remote updates and reopening. |
| 4 / 1 | 19/20/21 correct step matches; normalize padded/mixed-case step metadata in fixture only. | Threshold20; result Advanced, not main-stage completion; part2 visible on both clients. |
| 4 / 2 | Exact strings 15 and 4; swap, blanks, padding, 15.0, 04. | Only exact strings complete; Crystal2, clue154, stage5/Physician Room, timer reset. Reset returns to part1 and clears both text fields. |
| 5 | FLUOROQUINOLONES, SULFONYLUREAS, LINEZOLID, AZOLES, ELDERLY, IMPAIRED, REDUCTION; omit one letter/whole word; add unrelated cell; drag remove; try headers. | All seven and no extra cells; completed words green, incomplete/outside selections red; red wins overlap. Headers unplayable. Clue477; stage6; timer reset. |
| 6 / 1 | 32; 32.0 and padded input; clearly wrong and >0.0001 numeric error. | Advanced to diary page2 together, main stage remains6. |
| 6 / 2 | Field order 24,8,8,8; independently corrupt/blank each. | Advanced to page3 together; no premature reward. |
| 6 / 3 | Field order 40,20,40; independently corrupt/blank each; case/numeric tolerance fixtures. | Completed; Crystal3; stage7/Pharmacy; timer reset. Reset clears all eight fields and shows only page1. |
| 7 | Six dropdowns 1,2,1,1,1,1; change each independently. | Clue28; Puzzle8 opens; timer reset. All six references must exist: runtime skips null dropdowns. |
| 8 | 28, padded28, 028, 28.0, blank, wrong value. | Trimmed28 succeeds; Crystal4; stage9/Nurse Education Room; timer reset. Do not assert unassigned fourth trigger disappears. |
| 9 | lispro, NPH, glulisine, aspart, detemir, Lantus, regular, Toujeo; mixed-case; blank/wrong every active cell in fixture. | All active letters match case-insensitively; across/image hints switch; clue135; stage10; timer reset. Record panel visibility separately. |
| 10 / entry | Try syringe before chest has ever opened; try other puzzle triggers; open then close lid and retry syringe. | Dedicated syringe trigger and historical chest-open condition gate entry. |
| 10 / 1 | Dropdown0/wrong then index1 using dedicated action; press twice; close/reset/disconnect during two-second delay. | Correct selection broadcasts part2 after delay. Desired invariant: reset/disconnect must not reopen stale page; known local-delay recovery gap. |
| 10 / 2 | 8/9/10 correct step placements, one wrong/empty/occupied drop. | Threshold9; close panel; stage11/Morturary; timer reset. |
| 11 / 1 | 4/5 filled Stage1Slots; pad/mix case; substitute different lapse cards. | Stores normalized card answers; threshold5; Advanced to part2. Current self-comparison is primarily filled-slot validation, not independent clinical correctness. |
| 11 / 2 | Four linked Stage2Slot matches; 3 matches; wrong linked answer; missing link configuration. | Crystal5; configured world mover once per intended completion; CSV request. Current stage stays11 and timer is not terminal/stopped. Duplicate-submit behavior must be recorded. |

## Shared attempts, drag ownership, recovery and timing

| ID | Precondition / actions | Expected result / limitation |
| --- | --- | --- |
| N01 | A opens puzzle; B contributes shared text/dropdown/button/card input; B attempts managed Submit. | Same panel/results; only designated Submit visible; input contribution remains cooperative. Test method-level authority separately from button visibility. |
| N02 | A holds attempt, closes/reopens; B attempts new lock; granted participant reopens. | Owner/re-entry access preserved for same stage/owner; unrelated start denied with temporary warning. Wrong submission retains lock. |
| N03 | Two then three active actors each use turn; include one inactive actor; wrong submissions and later correct. | Round increments only when all active actors played; played flags clear; inactive actor cannot block next round. |
| N04 | B first-drags a card owned by A; release over free slot before transfer finishes. | Immediate local preview; transfer pending position not overwritten; deferred acceptance after drop dispatch; all clients converge. |
| N05 | Repeat N04 but occupy target from A before B's transfer; fail/deny ownership transfer. | Occupancy rechecked; failed transfer returns to drag origin, raycasts/canvas state usable; no duplicate card. |
| N06 | Drag into occupied slot, invalid area, alternate panel, right-click enlarge, front/back and reset, for every card behavior. | Correct per-behavior return/home/scale/image; drag-name tag appears for peers and clears at end; no transient click buffer accumulation. |
| N07 | Move same-name sibling card/slot hierarchies in fixtures; reopen hidden panels repeatedly. | Stable path disambiguation and ViewID-based state; no unrelated hidden slot acceptance. Disabling alone does not unregister DropSlot. |
| N08 | Disconnect lock owner before submit; disconnect unrelated player during attempt. | Abandoned lock releases and panel closes so teammate explicitly starts; unrelated departure preserves active attempt. |
| N09 | Submit correct main stage, disconnect submitter during two-second pending advance; separately disconnect master. | Current master executes PendingAdvanceStage/At when due; same result/stage on active clients; no stuck lock. Record duplicate effects; no atomic exactly-once claim. |
| N10 | Master switch while idle/locked/denied warning/round boundary. | Missing room props initialized, orphan lock repaired, pending advance processed; no unscaled warning stuck after clue pause. |
| N11 | Disconnect during each multipart page change (4,6,10,11), rejoin while another stays active. | Desired: current page/answers converge without reversing progress. Known durable-substage gaps must be reported per part. |
| N12 | Temporary loss then successful reconnect/rejoin within TTL; old actor still active then retry. | Cached room retained for active-joiner failure; one avatar respawns; normal spawn rather than saved-position restore. |
| N13 | Intentional leave, expired actor/room, last active player disconnect, fresh-process running-room selection. | Intentional cache cleared; expired rejoin returns Lobby; fresh player cannot use rejoin-only UI; cached name never guarantees recovery. |
| N14 | Rejoin after text/dropdown/button/card feedback changes, door unlock, reward, stage transition and story audio. | Durable properties/buffered effects restored where supported; no replay popup/reward/movement duplication. Historical audio/popups checked explicitly. |
| N15 | Interleave rapid A/B text edits including stale/duplicate sequence and result updates; disable/enable controls. | Per-actor stale sequence ignored; callback suppression prevents echoes; final text/color/state consistent. No cross-system cast exceptions. |
| N16 | Crossword text while analytics collects; word search while regular close-count events arrive. | Desired: independent protocols and validated payloads. Existing codes41/42 collide; diagnostics reproduce allocation failure. |
| T01 | Start GameScene; inspect15:00, cross05:00 and02:00; reset then cross again. | Correct threshold message once per timer cycle, ten-second configured banner; reset clears warning state. |
| T02 | Expire each stage0–11 on both clients, including every multipart page and cards owned by peer. | Current-stage retry only for1–11; master restarts; no earlier progress loss. Stage0 has no restart handler. |
| T03 | Complete1→2 and3→4; complete each other stage. | Exceptions keep deadline; other hooks reset, including11. Verify real deadline rather than display text alone. |
| T04 | Open clue (timescale0) near timeout, use Q, move/look/interact, close; trigger popup/autoplay. | Photon countdown continues; popup/comic unscaled flow runs; scaled delays/animations may pause; cursor/control state usable after close. |
| T05 | Expiry during pending advance and insulin delay; simultaneous final votes/leave. | Desired: no stale page reopen, double advance, stranded lock or reversed UI. Record races separately from nominal resets. |

## Clues, comics, inventory, story, guidance and layout

| ID | Actions | Expected result |
| --- | --- | --- |
| U01 | Open each gated comic; next/previous/first/last; A votes before B; reopen same comic. | Last-slide Close only; after vote visible disabled waiting count; new integer round prevents stale votes. |
| U02 | Open regular clue after slideshow on first/middle/last page; vote; receive remaining-count event/property update. | Slideshow deactivated/listener detached; shared Close visible; waiting text persists after vote; one handler responds. |
| U03 | Two/three players vote; inactive player leaves before/after vote; master changes; actor rejoins during gate. | Only active players required; round schema maintained; closes once all required votes exist; no permanently stuck button. |
| U04 | Collect clue twice; inspect locked icon, revealed icon/full image, badge; open detail; close locally. | Deduplicated collection; badge increments once and clears on viewing; locked slots remain clickable with explanation; detail creates no shared vote round. |
| U05 | Pick up key/clue from B while A master; click simultaneously; repeat after rejoin. | Shared inventory/clue additions converge; notification once as intended; master removes pickup; keys not consumed. |
| U06 | Collect each Time Crystal/password reward, inspect exact stage mapping, replay completion/rejoin. | Crystals after2/4/6/8/11; password clues after2/4/5/7/9; no duplicate new-clue popup from buffered replay. |
| U07 | Toggle every sidebar panel, switch panels, close all; enlarge card and close. | Single sidebar panel/blocker consistent; card enlargement local; cursor/world input recovery usable. |
| U08 | Complete each stage, observe all StorylineDirector directives, queue flashes, timeline/date labels, audio and delayed enter/exit popups. | Configured objects/components preserved/toggled; keep-enabled exceptions honored; renderers not accidentally disabled; stage8 exit popup uses38s, others configured timing. |
| U09 | Trigger each local comic with owned and remote Player colliders; non-Player collider; re-enter. | Current TriggerComic is local, once-only, no ownership filter. Record undesired remote activation; do not assume broadcast. |
| U10 | Nurse dialogue7 timed slides while timescale0; disable/re-enable; ending2 slides. | Autoplay/audio unscaled and stops on disable; ending exits each client independently to Lobby. |
| U11 | At stages3/5/7/9/11 traverse Canteen7 / Physician4 / Pharmacy8 / NurseEducation6 / Morturary6. Trigger out of order and arrive within1.5m, including stairs. | Route order cannot skip; arrow horizontal regardless of pitch/floor; hides at final point; clears at other stages. |
| U12 | Wrong keypad, retry after display delay, correct325/154/477/28/135; both clients interact and rejoin. | Denied clears; max9 digits/backspace; Granted latches; assigned door leaves unlock/open/shared key as configured. |
| U13 | Hard-locked/key-required/mirrored/left/right doors, instant/smooth, repeated toggle, disable while highlighted. | Correct deny/prompt/hinge direction, persistent opened history, cleanup; unlock does not consume inventory. |
| U14 | Final world mover invoked twice; rejoin mid-motion and after arrival. | No restart after reached; network-time interpolation converges; record incomplete mid-motion timestamp recovery. |
| U15 | Run every presentation/input case at all five aspect ratios above. | Text, Close/waiting label, controls, chat, room entries and cards within viewport, readable/clickable; no overlap/truncation or unreachable navigation. |
| U16 | Dates around day/month/year rollover using controlled test clock/culture, no OS settings automation required. | Diary/prescription/helper formats/offsets match local clock; master-broadcast story dates distinguished from per-device labels. |
| U17 | Auxiliary rotation puzzle: rotate four times and reset repeatedly. | Quarter turns wrap; solution checks match visual orientation. Not an extra registered runtime stage; investigate reset/internal-rotation divergence. |

## Chat

| ID | Actions | Expected result / limitation |
| --- | --- | --- |
| C01 | Before username, then after name; send blank/padded text with button/Enter. | Connect waits for name; blank ignored; trimmed message published once; no duplicate UI listeners. |
| C02 | A master and B joiner chat in waiting room, then GameScene; inspect dropdown/subscriptions. | Current asymmetry: A Global, B room in Lobby; both room in GameScene. Record split conversation as known gap. |
| C03 | Hide/show chat; receive several messages, open, switch scenes and repeat session. | Unread badge increments while hidden, clears on open; persistent manager rebinds UI; collapse/send/input usable. |
| C04 | Lose Chat service while PUN stays connected; recover PUN separately; master migration. | Independent chat connection must be observed; no assumption PUN recovery restores Chat. Record incomplete reconnect/private-message behavior. |

## Analytics and delivery (synthetic data only)

| ID | Actions | Expected result / limitation |
| --- | --- | --- |
| A01 | Synthetic entry/wrong/correct/timing/input events for every stage/part and two submitters. | Stage0/2 excluded; tagged4/6/10/11 parts; crossword part0; team rows include submitter/part; negative elapsed clamps. |
| A02 | Export synthetic fixture session to test-owned output; inspect both CSV sections with CSV parser. | Correct identifiers/tries/correct/wrong/variable columns; first successful snapshot retained; individual suppressions4p2/5/6p1–3/8/9; quotes/comma/newline escaped. |
| A03 | Compare individual teammate edits with submitter snapshots, total timers and first row aggregate. | No claim every teammate contribution exported; aggregate sum only first team row; durations text MM:SS; no formula-sanitization guarantee. |
| A04 | Complete11; early leave/quit; RestartLevel; standalone QuitButton; OS close. | Record exact export call-path coverage; once-per-persistent-logger behavior; no guaranteed OS-close flush. Never trigger real mail as incidental test. |
| A05 | Force export failure, retry; enter second room same process. | Desired: retry/new-session isolation. Existing hasWrittenCsv/date/dictionary lifecycle does not supply this; record defect and preserve first file. |
| A06 | Migrate master after historical events; organiser stays/leaves; inspect synthetic export. | Current history/credentials do not migrate. Only organiser who is current writer and remains in room eligible for SMTP; do not assume full history. |
| A07 | With verified dedicated test recipient and secure test sender config, successful direct SMTP then failure/interrupted quit. | Attachment synthetically generated; TLS; failure recorded without CSV/password contents; no retry/durable-shutdown guarantee. Check recipient logging remediation separately. |
| B01 | Dedicated test API: run Test-EmailApiValidation.ps1, optionally rate limiter. | health200/ok; non-form/missing/empty/oversized/wrong-extension400; rate429 after20/minute, queue0. No valid upload/SMTP in script. |
| B02 | After safe test configuration, submit1-byte and exactly5MiB .csv, uppercase.CSV; filename with path; blank room/date. | Current contract accepts nonempty≤5MiB CSV suffix, base attachment filename, awaits SMTP then204; room/date/schema currently unvalidated. Dedicated test recipient only. |
| B03 | Test SMTP rejection/network timeout/cancellation, invalid host/port startup, HTTPS redirects and multipart parse failure. | Validated options; appropriate error/exception path; no false204 before SMTP completes. Size check after form parsing is not total request limit. |
| B04 | Inspect Unity runtime with SmtpEmailSettings enabled/disabled and endpoint changed. | Settings validation alone does not change active direct-SMTP path; no HTTP Unity caller currently exists. |

## Completion record

Attach automated XML counts and manual per-ID status: Pass / Fail / Not run /
Blocked, with reason and reproduction evidence. Report known-gap failures
separately, but retain them. A green offline suite is not a two-client acceptance
pass, visual-device pass, backend-delivery pass or proof of complete recovery.
