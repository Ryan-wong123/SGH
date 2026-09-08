# AGENTS.md

## Repository purpose

This is the SGH cooperative educational puzzle game. It is a Unity 6 URP project using Photon PUN/Realtime and Photon Chat, with an optional ASP.NET Core CSV-email service in `backend/SghCsvEmailApi`.

Read [PRD.md](PRD.md) for expected game behavior and [architecture.md](architecture.md) for runtime ownership, state, and integration boundaries before changing multiplayer flow.

Documentation baseline: 8 September 2026. PRD.md describes player behavior and exact configured puzzle rules; architecture.md describes implementation and known recovery/data gaps. Distinguish intended invariants below from existing code that does not yet satisfy them. Verify script behavior and Inspector wiring before updating either document.

## Game flow and change map

- Enabled scenes: Lobby -> GameScene -> local ending-comic exit to Lobby. Avatar indices are Doctor=0, Nurse=1, Therapist=2; no role-specific abilities or exclusive reservations exist. GameManager spawns through Photon using sorted actor number and four configured spawn points.
- The home form collects username, sender email, masked app password, and recipient in sequence. SessionSmtpCredentials keeps them only in runtime memory; SessionOrganizerActorNumber identifies the creator separately from the replaceable master.
- Runtime stages: 0 opening comic; 1 SymptomsCardManager; 2 BearCardManager; 3 PictureGridManager; 4 Activity2PuzzleManager; 5 WordSearchManager; 6 DiaryPuzzleManager; 7 RiskTable; 8 PrescriptionManager; 9 CrosswordManager; 10 InsulinPuzzleManager; 11 DeathLapsesManager. Folder/activity and panel numbers are not reliable stage identifiers.
- Stages 4, 6, 10, 11 have multiple parts. `IPuzzle` contains only `IsSolved()`; resets are dispatched separately by PuzzleStateManager. Preserve part visibility, answer snapshots, and reset behavior together.
- GameScene timer is 900 seconds, with warnings at 300/120 seconds. Stages 1->2 and 3->4 keep the current timer; most other completion hooks reset it. Expiry retries the current stage, not a global loss. Stage 0 has no timeout reset; stage 11 completion does not stop the timer or set a terminal flag.
- Standard completion delay is two seconds. Time crystals are awarded after stages 2, 4, 6, 8, 11; password clues after 2, 4, 5, 7, 9. Exact mappings and answers are in PRD.md.
- Guidance starts at stages 3/5/7/9/11 using Canteen, Physician Room, Pharmacy, Nurse Education Room, and `Morturary`. Preserve that last serialized spelling unless migrating both code and scene. Routes are local authored waypoints, not NavMesh paths.
- Puzzle 11 triggers a world mover and CSV request. The separate End Game Comic has two slides and returns each player to Lobby independently; its all-player close gate is disabled. Do not treat the Puzzle 2 debug win message as terminal behavior.
- PlayerController, CrosshairInteractor, PlayerTagDisplay, ObjectivePointer, InventorySystem, and ClueSystem own player-facing state; PuzzleTrigger, PickupItem, DoorInteractable, and NavKeypad components connect world interactions to it.
- StorylineDirector owns stage-driven object/component toggles, queue/status text, audio, and delayed popups. TriggerComic is local collider activation, without an ownership filter or direct broadcast. CluePopupUI is an unscaled FIFO presentation queue; callers decide synchronization.
- ChatManager has a separate persistent Photon Chat connection. Global is used outside rooms and by the Lobby master; joiners and GameScene participants use the room name. Do not assume waiting-room chat is already unified or chat recovery follows PUN automatically.
- Useful filename/type mismatches: GameTimeManager/GameTimerManager, PuzzlePanelTrigger/PuzzleTrigger, SlideShowUI/SlideshowUI, SideBarMenuManager/SidebarMenuManager, CrossHairInteractor/CrosshairInteractor, PhotonSyncedInputField/PhotonSyncedTMPInputField, MovementTargetNetworked/MovementTargetForward.

## Working conventions

- Make project code changes in `Assets/Scripts`; treat `Assets/Photon`, `Assets/LeanTween`, and `Assets/TextMesh Pro` as third-party code unless a task explicitly requires a vendor change.
- Unity scene and prefab YAML are source-controlled assets. Preserve serialization, object references, and `.meta` files. Do not hand-edit serialized YAML unless the task genuinely requires it and the exact target has been inspected.
- Match the surrounding style in files you touch. Existing scripts use explicit null checks and extensive inline comments; avoid reformatting unrelated code.
- Keep a change narrowly scoped. A scene/prefab assignment often matters as much as the C# code.
- Do not modify generated folders or files: `Library/`, `Temp/`, `Logs/`, `obj/`, `.vs/`, generated DLLs/PDBs, or Unity-generated build output.
- Do not discard or overwrite existing user changes. Inspect `git status` and `git diff` before and after work.

## Build and verification

Run from the repository root on Windows PowerShell:

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

The compile can update tracked `obj/` artifacts. Restore only those generated artifacts after a successful build:

```powershell
git restore -- obj
```

Only restore generated artifacts that were clean before the build; preserve any pre-existing user edits. For backend code changes also run `dotnet build backend/SghCsvEmailApi/SghCsvEmailApi.csproj --no-restore` when dependencies are available. Documentation-only work needs source/reference checks and the relevant compile baseline, but does not imply a completed Play Mode test.

For changes that affect a puzzle, interactable, clue UI, room flow, or Photon state, compilation is necessary but not sufficient. Verify in Unity Play Mode with the affected scene. For shared-state changes, use two clients in the same room.

Minimum multiplayer regression checks:

1. Create/join/start a room and confirm all clients load `GameScene`.
2. Verify a non-master player can interact and a master-client switch does not block progress.
3. Verify a temporary disconnect does not leave the puzzle lock, delayed stage advance, or clue close gate stuck.
4. Verify active players see the same puzzle result/stage.
5. Verify a regular clue after a slideshow has a visible Close button and retains its waiting-player message after a vote.
6. Verify every affected multipart page and timer reset, including cards owned by another client and a disconnect during the insulin part-1 delay.
7. Verify keypad denial/retry/unlock, guidance across floors, final puzzle completion, independent ending exit, and a second room/session in the same process.
8. For analytics changes, inspect synthetic CSV rows and exclusions, early leave/quit, master migration, and send-failure behavior. Send email only to explicitly configured test recipients; never use real gameplay CSVs as test fixtures.

## Photon rules

- `PuzzleStateManager` is the shared puzzle-flow authority. Register a new puzzle's panel, `IPuzzle` manager, reset behavior, stage completion, clue reward, and story configuration there and in `GameScene`.
- Do not use a local coroutine as the only source of truth for a stage transition. Use Photon room properties for recovery-critical state; the current pending-transition contract is `PendingAdvanceStage` plus `PendingAdvanceAt`.
- Only the master should decide globally shared transitions, timer starts, lock repair, and final CSV generation. Other clients request/synchronize rather than independently advancing state.
- Treat `Player.IsInactive` as absent when determining required puzzle turns and close votes. A disconnected player must not block the remaining team.
- Preserve compatibility when changing Photon event assignments, and inspect all producers/consumers together. Existing conflicts: `41` is used by analytics AND crossword text; `42` by clue waiting counts AND word-search state; `43` by crossword result. They are not safe shared protocols. Do not allocate new events to those codes; a fix must migrate every affected sender/receiver and validate payload types.
- Use buffered RPCs only for effects late joiners need. Avoid buffering transient clicks, animation states, or repeated UI updates.
- Do not reuse a room/property key for unrelated systems. Namespace new properties and document their writer/reader/recovery behavior.
- CurrentStage is a local field changed by buffered RPCs; it is not a dedicated room snapshot. PendingAdvanceStage/At are room properties written by successful submission and executed by the master. Do not claim this supplies atomic exactly-once completion.
- Multipart page changes and the insulin local delay lack complete durable recovery. Implement new recovery-critical substages as explicit room state rather than copying that limitation.
- Puzzle lock/submit ownership and individual card PhotonView ownership are separate. All players can receive the puzzle panel and contribute shared input. Preserve first-drag local preview, deferred slot acceptance after ownership transfer, occupied-slot checks, and failed-transfer rollback.
- Shared control keys use PhotonView IDs; draggable layouts also use hierarchy paths. Preserve pathRoot, relevant hierarchy names, scene view IDs, tags, and callback suppression/sequence handling. See architecture.md for key prefixes.
- DropSlot.allDropSlots unregisters on destruction, not disable. Some validators/reset searches depend on active objects or global slot membership. Test hidden panels and repeated entry instead of assuming collections are scoped to the current puzzle.
- Rejoin caches require another active player. GameManager removes inactive avatars and respawns on rejoin, without a saved-position restore. Historic analytics, local comic flags, and organiser credentials are not automatically migrated with the master.

## Clue and UI rules

- `ClueUI` and `SlideShowUI` share a physical Close button under the `Clue` root in `GameScene`.
- A regular clue must call the standard close visibility path and must deactivate active slideshow controllers before taking the shared button. Never add independent handlers that let both controllers respond to one click.
- The multiplayer close gate must leave the Close button visible after a local vote, update its remaining-player text, and close only once all active players have voted.
- Check all new/changed screen-space UI at multiple aspect ratios. In particular, test 16:9, 16:10, ultrawide, tablet portrait, and mobile landscape; do not assume the reference resolution is the player's resolution.
- Lobby room entries are instantiated under `contentPanel`. Ensure the `Lobby` content object has an appropriate layout component or explicitly position entries; otherwise clones overlap and only the last rendered room appears selectable.
- Regular-clue votes are actor-suffixed room properties; comic votes are player properties. Both store round numbers, not simple permanent booleans. Keep their distinct schemas and exclude inactive players.
- Collected-image viewing is local; normal comics gate last-slide Close, while the ending comic exits locally. Preserve this distinction when modifying the shared button.
- Time.timeScale is not a Photon timer pause. Clues pause scaled time locally while countdown, popup queue, and comic autoplay use network/unscaled time. Test cursor/Q, world interactions, animation, and timer together.

## Security and data rules

- Never commit real SMTP passwords, access tokens, API keys, or personal recipient addresses. Use local/user secrets or deployed environment variables.
- Never place credentials in Photon room/player custom properties, Unity `Resources`, ScriptableObject assets, scenes, prefabs, logs, or documentation.
- Treat gameplay CSV output as sensitive. Do not log its contents or transmit it to an unverified endpoint.
- The Unity direct-SMTP path and the .NET email API are currently separate. Do not extend both paths for a feature without explicitly deciding which production route is authoritative.
- GameSessionCsvLogger excludes stages 0/2 and suppresses individual rows for 4/part2, 5, 6/parts1-3, 8, 9. Preserve these reporting choices unless explicitly changing requirements. Team rows include submitter and part; expected answers derive from first successful snapshots.
- Event 41 aggregation is in master-local memory, not durable server analytics. Export writes Downloads CSV once per persistent logger; a new-room reset, historic-master migration, and reliable async email shutdown are not currently implemented. Do not advertise them as working.
- Source constants in the optional backend and recipient-address logging are existing remediation items, not approved patterns to copy. Never paste their values into docs, commands, tests, or user-facing output.

## Backend rules

- `backend/SghCsvEmailApi` targets .NET 10 and exposes `GET /health` and `POST /api/gameplay-csv`.
- If changing the API contract, update the Unity caller in the same change only after confirming that Unity has a real HTTP caller. `SmtpEmailSettings` currently validates backend configuration but is not used by `GameSessionCsvLogger`.
- Keep request limits, file-size validation, and HTTPS requirements intact or strengthen them.
- Actual API contract: form file `csv`, roomName, playedAt; nonempty .csv filename and at most 5 MiB; await SMTP then 204. Fixed-window limiter is 20/minute with no queue; no per-user/IP partition is configured. The size check is after form parsing, not a custom total request limit.
- GmailOptions configures only host/port. Sender/password/fixed recipient currently come from CsvEmailSender constants; migrate to secure configuration when touching delivery. No recipient parameter, authentication, database, or retry queue is implemented.
- The backend README contains stale integration/credential instructions. Do not follow its recommendation to put passwords into source. SmtpEmailSettings is unused by the active Unity logger; its enable flag does not disable direct SMTP.

## Before handing off

- Run `git diff --check`.
- Run the relevant compile/test command.
- Confirm `git status --short` contains only intentional source/asset/documentation changes.
- State what was tested and what requires Unity/two-client manual verification.
