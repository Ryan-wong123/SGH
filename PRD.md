# SGH Game Product Requirements Document

## Scope and evidence

Updated 8 September 2026 from project-owned scripts, Lobby/GameScene serialization, relevant prefabs, project settings, and `backend/SghCsvEmailApi`. This describes implementation, with requirements and gaps identified separately. Source inspection is not a completed multiplayer playtest. Imported packages, artwork, and vendor demos are dependencies, not additional SGH game modes.

See [architecture.md](architecture.md) for runtime contracts and [AGENTS.md](AGENTS.md) for maintenance rules.

## Product and player journey

SGH is a cooperative first-person hospital puzzle game about hypoglycemia, diabetes medicines, insulin, and lapses surrounding a patient's death. Players investigate through documents, conversations, comics, and eleven ordered puzzles. Story text moves between the present, two hours before death, and two weeks before death. Routes lead through the canteen, physician room, pharmacy, nurse education room, and mortuary.

The team shares progression, supported puzzle inputs, card layouts, clues, and inventory rewards. Each player has their own avatar, camera, navigation progress, and reading controls. Photon provides gameplay networking and a separate Chat service; there is no project-owned dedicated gameplay server or database. Clinical answers documented here describe game validation, not independently reviewed clinical guidance.

### Session setup

1. Connect to Photon with game version `1.0` and a device-local persistent user ID.
2. The home form asks sequentially for username, sender Gmail address, Gmail app password, and CSV recipient. It reuses one input field and masks the password. This flow currently applies generally, not only to the eventual organiser. Credentials remain in runtime memory.
3. Create a named room with dropdown-selected capacity or select a listed room and enter its password. Creation generates six characters. Password checks use lobby properties and are a convenience gate rather than secure authentication.
4. Choose Doctor, Nurse, or Therapist (indices 0, 1, 2). Selection is highlighted and stored in player properties. No distinct abilities or exclusive role reservations are implemented. Invalid/missing selection defaults to Doctor.
5. The waiting room shows names, population, and password. Only the master starts; `StartGame` has no all-ready requirement. The room stays visible and open, is marked as in progress, and all clients load GameScene through Photon. The Lobby lists it as a rejoin option; only a player with that room cached locally can select its rejoin path.
6. Each client spawns its avatar after a short readiness delay, using sorted actor number to select one of four spawn points; overflow uses the first point.

The room allows inactive actors to rejoin for ten minutes and retains an empty room for five minutes. Clients cache the room name even if they are the last active player; an empty room can still expire, and a cached name does not guarantee that Photon can restore the actor. This is temporary session recovery, not save/load.

### Player controls

| Action | Behavior |
| --- | --- |
| Movement / look | Horizontal and Vertical input axes, mouse look, CharacterController movement, walking animation. |
| Free cursor | Q toggles the cursor and suppresses local movement/look while unlocked. |
| World interaction | E uses the crosshair target; prompts and highlighting identify usable objects. |
| Keypad | Left-click world buttons to enter numbers, backspace, and submit. |
| Cards | Drag into slots; occupied/invalid drops return according to configured card behavior. Right-click enlarges a card image. |
| Sidebar | Toggle configured inventory, clues, instructions, settings, and other panels with a shared blocker. |
| Identity | Remote avatars have camera-facing name tags; shared dragged cards show the dragging player's name. |

Only the owned avatar runs movement/look; remote cameras are disabled. Cursor state and panel visibility are separate systems, so opening a panel is not a universal input-lock contract.

### Guiding arrow

The floating arrow stays horizontal and points to authored waypoints through corridors and stairs. It runs at stages 3 (Canteen, 7 points), 5 (Physician Room, 4), 7 (Pharmacy, 8), 9 (Nurse Education Room, 6), and 11 (`Morturary`, serialized spelling, 6). Other stages clear its target.

Each client advances its route within 1.5 metres of the current waypoint. Matching trigger volumes can also report arrival; out-of-order arrivals cannot skip the route. The arrow hides at the final point. This is authored guidance, not automatic pathfinding.

## Puzzle sequence

`PuzzleStateManager` stage numbers are authoritative; activity folder and panel names sometimes differ. Standard main-stage success waits two seconds in GameScene. Multipart success can reveal another page without changing the main stage.

| Stage / content | Mechanism and actual success rule | Completion effect |
| --- | --- | --- |
| 0: Opening comic | Nine slides; all active players reach the last slide and vote Close. | Master sets stage 1. Not scored. |
| 1: Decode hypoglycemia symptoms | `SymptomsCardManager`: every registered slot must contain a first card whose symptom matches its configured answer list. Serialized `required` is unused by this validator. | Opens Puzzle 2; same timer continues. |
| 2: Align the palette | `BearCardManager`: at least 10 correct first-card placements against slot symptom lists. | Time Crystal1, clue `325`, stage 3, Canteen route, timer reset. Excluded from CSV. |
| 3: Fuel up against low sugar | `PictureGridManager`: select exactly the correct pictures, without extras or omissions. Scene has 20 buttons, correct zero-based indices 0, 1, 7, 8, 17, 19. | Opens Puzzle 4; same timer continues. |
| 4: Survival code | `Activity2PuzzleManager`: part 1 requires 20 correct card/slot step matches; part 2 requires exact strings `15` and `4`. | Part 1 broadcasts part 2. Final success gives Time Crystal2, clue `154`, stage 5, Physician Room route, timer reset. |
| 5: Hidden interactions | `WordSearchManager`: select all seven target words and no unrelated cells. Fixed 21×21 grid includes coordinate headers. | Clue `477`, stage 6, panel closes, timer resets. |
| 6: Glucose Diary | Three pages, configured field-order answers: `32`; then `24, 8, 8, 8`; then `40, 20, 40`. Trims text, supports case rules and numeric equivalence within 0.0001. | Correct pages advance together. Final page gives Time Crystal3, stage 7, Pharmacy route, timer reset. |
| 7: Medicine risk table | `RiskTable`: six dropdowns match configured indices `1, 2, 1, 1, 1, 1`. Null references are skipped, making scene wiring essential. | Clue `28`, opens Puzzle 8, timer reset. |
| 8: Insulatard dose | `PrescriptionManager`: trimmed answer must be `28`. | Time Crystal4, stage 9, Nurse Education Room route, timer reset. Fourth trigger reference is unassigned, so this hook removes no trigger. |
| 9: Decode insulin | `CrosswordManager`: every active letter in a fixed 13×13 crossword must match case-insensitively. | Across-hint switches to image-hint; clue `135`, stage 10, timer reset. Hook does not explicitly close the whole crossword panel. |
| 10: Insulatard vial sequence | Syringe dropdown index 1 through a dedicated submit action; correct selection opens part 2 after a local two-second delay. Then require nine correct step-card placements. | Closes panel, stage 11, mortuary route, timer reset. |
| 11: Hidden lapses | Five part-1 placements store normalized `lapsesAnswer`; part 2 requires four cards matching stored answers through linked slots. | Time Crystal5, trigger hiding, configured world-object movement, CSV request. Stage stays 11. |

Word-search targets: FLUOROQUINOLONES, SULFONYLUREAS, LINEZOLID, AZOLES, ELDERLY, IMPAIRED, REDUCTION. Dragging adds/removes cells. Complete words turn green; selected cells in incomplete words or outside target words turn red. Red takes precedence where an incomplete word overlaps a complete one.

Crossword words: lispro, NPH, glulisine, aspart, detemir, Lantus, regular, and Toujeo. Empty or wrong letters fail. Most puzzles show green/red feedback; several card managers clear wrong tint after one second.

Puzzle 11 part 1 normally compares each card with the answer just stored from that same card. It primarily validates filled-slot count rather than an independent predefined answer key. Rotation-tile scripts also exist but are not registered as another stage.

## Cooperative attempts and transitions

A world trigger requests an attempt from `PuzzleStateManager`. The master manages the room lock and actor owner. Opening a puzzle is broadcast to all participants. Supported controls let teammates contribute, while the designated owner receives managed Submit buttons. The lock controls attempts/submission, not exclusive ownership of every input.

Starting marks `PlayedRound`. The owner can close/reopen the same attempt; clients already granted re-entry can reopen that shared attempt too. Others trying to start a held attempt receive a temporary warning. Successful main-stage completion releases the lock. When all active players have used their turn, the master increments `Round` and clients clear played flags. Wrong submissions do not automatically release the lock.

Some triggers show a clue before opening the puzzle through its close callback. First-view memory is local to the trigger. Correct main-stage answers store a pending stage and Photon-time deadline in room properties. The current master completes that transition even if the submitting client disconnects. Multipart page changes use separate RPCs and do not all have durable recovery.

## Countdown, losing, and winning

GameScene configures **15 minutes (900 seconds)**. The timer starts with the game timer component, including the opening state. Warnings appear at five minutes (Time's almost up!) and two minutes (Puzzle is going to reset!).

Expiry resets the current stage's configured cards, inputs, selections, or multipart page, then the master restarts the timer for stages 1–11. It does not erase all earlier progress, remove all keys, kill a player, or load a loss scene. Stage 0 has no reset handler and does not automatically restart at expiry. Stages 1→2 and 3→4 share their current timer; most other completion hooks reset it.

The countdown uses Photon time. Local `Time.timeScale = 0` for a clue does not pause it. There is no separate global loss state, limited-lives system, or permanently failed session state.

Puzzle 11 completion finishes the puzzle chain and requests analytics, but does not set a terminal won flag or stop the timer. The two-slide End Game Comic is separately registered as `Title Deed Trigger`. Its last-slide Close leaves the room and loads Lobby independently for each client; this comic has no all-player gate. The optional score-summary panel is not automatically invoked by Puzzle 11 completion. A debug message saying You win after Puzzle 2 is not the ending.

## Doors, inventory, clues, and story

Keypads accept up to nine input characters, backspace, and enter. They compare a parsed integer with their configured combination. Incorrect entries show Denied, play feedback, then clear after the display delay. Success latches Granted, unlocks assigned door leaves, can remove key requirements, and can open the doors automatically. GameScene combinations are `325`, `154`, `477`, `28`, and `135`; the standalone prefab default is different.

Doors separately check hard-lock and inventory-key conditions. Allowed E interaction toggles a hinge with instant or smooth rotation. Inventory is a deduplicated set of item names; keys are not consumed. Time crystals, keypad inventory rewards, and password-clue images are distinct concepts.

The clue grid includes envelope, Glucose Diary, prescription, Intervention Note, Property Bag, and five password clues. Locked slots remain clickable and explain their state. Collected slots reveal an icon and full-image view. New clues increment a badge; viewing clears it. Pickups synchronize collection and notifications, optionally open a display, then ask the master to remove the world pickup.

`StorylineDirector` applies stage-configured object activation, component/trigger disabling, queue board text/flashing, timeline text, audio, and delayed popups. Stage 0 opens Comic 1; stage 9 opens the nurse dialogue. The nurse dialogue autoplays seven timed slides with audio. Other story comics use previous/next controls. Local collider triggers also activate comics. Diary/prescription/date-helper labels use dates relative to the device clock.

Regular clues and comics share a physical Close button. Gated presentations wait for all active players; a local vote leaves Close visible, disabled, and showing the remaining count. Inactive players are excluded. Slides show Close only on the final page; page navigation itself is local. Collected-image detail viewing closes locally instead of creating another team gate.

Notifications use a FIFO popup queue with unscaled fade/slide timing, including while gameplay is paused. Pickups, password rewards, locked clues, and story messages feed it. Turn warnings, timer banners, chat badges, and clue badges are separate mechanisms.

## Chat and reconnection

Photon Chat uses a separate persistent connection, waits for a nickname, services messages every frame, and rebinds UI across scenes. Send or Enter publishes trimmed text; hidden messages increment unread count, cleared when opened.

Outside rooms the channel is Global. In Lobby, the master also uses Global while joiners use the room name. In GameScene all participants use the room-name channel. This waiting-room asymmetry can split the conversation. The dropdown displays the automatic channel; private-message callbacks are empty. Independent chat reconnection is incomplete.

Gameplay recovery first attempts ReconnectAndRejoin, then reconnect/fresh connection followed by cached-room rejoin. Failed gameplay rejoin returns to Lobby. Intentional leave clears the cache. The master removes inactive players' avatar objects; rejoining spawns a replacement if no owned model exists. Position is not restored from a saved checkpoint.

On membership changes the master releases a lock whose owner is absent, allowing a teammate to start the attempt. An unrelated departure does not end an active attempt. Inactive players do not block turn rounds or close votes. Room properties and buffered effects recover portions of state; this is not complete restoration of all UI, multipart pages, historical analytics, or organiser credentials.

## Analytics and automatic email

The persistent logger gathers entry, submission, elapsed-time, and supported control-input events. Non-masters send reliable event 41 to the master for in-memory aggregation. Eligible completion/leave/quit paths export UTF-8 `gameplay_<room>_<timestamp>.csv` into the user's Downloads directory.

The file has `INDIVIDUAL_PLAYER_INPUTS` and `TEAM_SUBMISSIONS` sections. They include date, room, player/submitter, puzzle identity, tries, correct/wrong counts, and variable wrong-answer columns. Team rows add the first successful snapshot as expected answers, elapsed time, and summed team time on the first row. Team grouping includes submitter, so there can be multiple rows per puzzle/part.

Stage 0 and Puzzle 2 are excluded. Individual rows are suppressed for Puzzle 4 part 2, Puzzle 5, all three diary parts, Puzzle 8, and Puzzle 9. Remaining individual counts belong to the submitter; they do not measure every teammate's contribution. Latest control values are collected internally but not exported as a complete raw input history. Team timing is accumulated attempt time, not guaranteed session wall time.

Automatic delivery currently means the master writes the file and, if it is also the recorded organiser still in the room with runtime SMTP details, sends it through Gmail SMTP over TLS. Failures are logged; there is no retry queue or guaranteed send completion before quit. Master migration does not transfer analytics history or credentials. The persistent logger lacks a new-room reset, so repeated sessions in one process need verification.

The optional ASP.NET Core API can accept and email a CSV, but Unity currently does not call it. Changing `SmtpEmailSettings` does not alter direct SMTP delivery. Its existing backend README has stale integration and credential instructions; the verified contract is in architecture.md.

## Acceptance requirements and known gaps

Required verification, not asserted completed testing:

1. With two clients, create/join/start, choose every avatar, and verify owned controls, remote models/tags, shared inputs, and chat channel behavior.
2. Solve every stage and part; verify rewards, next panel, guidance route, story effects, and the timer exceptions above.
3. Make wrong submissions and expire each stage; ensure retry leaves usable cards/controls and no orphaned state.
4. Disconnect the lock owner/master during drag, multipart change, delayed advance, and close vote. Verify remaining-team progress and rejoin without duplicate avatars or reversed state.
5. Open a regular clue after a slideshow; verify Close visibility/count, local image close, and independent ending exit.
6. Verify keypad rejection/retry, shared unlock, and rejoin after doors open.
7. Check guidance across floors and UI at 16:9, 16:10, ultrawide, tablet portrait, and mobile landscape. Keyboard/mouse controls do not imply implemented touch controls.
8. Validate synthetic CSV data, exclusions, early exit, master migration, second-session behavior, and explicitly configured test email delivery.

Known gaps: event 41 overlaps analytics/crossword text; event 42 overlaps word-search/clue-close data; durable main-stage transitions do not cover every substage; validation is client-side; Puzzle 11 has no terminal latch; analytics/email are not durable across master migration. Backend credential constants and recipient logging require production remediation. Lobby content layout must prevent room-entry overlap. None of these gaps is fixed by this documentation update.
