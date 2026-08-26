# SGH Game Product Requirements Document

## 1. Product summary

SGH is a cooperative, first-person, multiplayer educational puzzle game built in Unity. A team enters a private Photon room, selects characters, explores a hospital-themed scene, completes a sequence of eleven clinical-learning puzzles, collects clues and keys, and reaches the end-game state together.

The game is designed around shared progression: the room has one current puzzle stage, puzzle interactions are coordinated across players, and key story/clue events are synchronized through Photon.

## 2. Players and session model

- A player supplies a display name, joins the Photon lobby, then creates or joins a room.
- A room has a generated six-character password, a configurable player capacity, a 10-minute player rejoin TTL, and a five-minute empty-room TTL.
- The room creator is the Photon master client and can start the game. Starting the game hides the room from the lobby and loads `GameScene` for the room.
- Each player chooses one of the configured character prefabs. `GameManager` spawns that player at a deterministic spawn point based on the sorted Photon player list.
- A temporary disconnect does not end the session. Players can rejoin their room while its player TTL is valid. The remaining active players must still be able to progress.

## 3. Core player experience

1. **Lobby and team formation**
   - Connect to Photon, set a nickname, create or select a room, provide its password, and wait for the master client to start.
   - Use room chat while waiting or playing.

2. **Explore and interact**
   - Move a first-person character around the scene.
   - Use the crosshair interaction prompt to collect items, open doors, trigger puzzles, operate keypads, and follow objectives.
   - Use sidebar UI for inventory, clues, instructions, settings, and chat.

3. **Solve the shared game**
   - Progress through the ordered puzzle stages.
   - At most one active player owns a new puzzle attempt at a time. The lock owner can close and re-open their own active attempt; other players are told that the puzzle is held.
   - Puzzle input and answer feedback use Photon synchronization where the individual puzzle requires it.
   - Correct solutions advance the room, reward configured clues, update story objects/objectives, reset the shared countdown, and may unlock keys or doors.

4. **Read clues and story panels**
   - Regular clue panels, image-detail clues, and comic/slideshow panels are local views of shared game content.
   - In multiplayer, configured close gates require every active player to click Close. After a player votes, the button remains visible, displays the remaining player count, and is no longer interactable for that player.
   - Inactive/disconnected Photon players are not expected to vote, so they cannot indefinitely block a clue or comic from closing.

5. **Finish and record the session**
   - Puzzle completion and player inputs are collected into per-player and team rows.
   - The master client writes a gameplay CSV at the final stage, on an intentional room exit, or when quitting through game UI.
   - The current implementation writes the file to the current user's Downloads folder and conditionally emails it from the session organiser client when runtime SMTP credentials are available.

## 4. Functional requirements

### 4.1 Multiplayer and recovery

- Scene changes must be synchronized by Photon master-client authority.
- Puzzle stage, puzzle lock, lock owner, turn round, and delayed stage transition state must be stored in Photon room/player properties or buffered RPCs as appropriate.
- A correct submission must survive the submitting player's disconnect during its configured transition delay.
- When the lock holder disconnects, the new/current master must reassign the lock to an active player when possible or release it. Active players must be able to continue.
- A master-client switch must preserve/recover pending stage advance work.

### 4.2 Puzzle progression

The ordered stage mapping in `PuzzleStateManager` is authoritative. Folder/activity names are content labels and do not always match the stage number.

| Stage | Runtime manager / content |
| --- | --- |
| Puzzle 0 | Opening comic / pre-puzzle state |
| Puzzle 1 | `SymptomsCardManager` |
| Puzzle 2 | `BearCardManager` |
| Puzzle 3 | `PictureGridManager` |
| Puzzle 4 | `Activity2PuzzleManager` |
| Puzzle 5 | `WordSearchManager` |
| Puzzle 6 | `DiaryPuzzleManager` |
| Puzzle 7 | `RiskTable` |
| Puzzle 8 | `PrescriptionManager` |
| Puzzle 9 | `CrosswordManager` |
| Puzzle 10 | `InsulinPuzzleManager` |
| Puzzle 11 | `DeathLapsesManager` |

Each puzzle must support the `IPuzzle` contract where applicable: evaluate completion and reset state when the shared timer expires.

### 4.3 UI and accessibility

- Gameplay UI uses Unity Canvas Scaler configuration from `GameScene`; screen changes must be checked at common 16:9, 16:10, ultrawide, tablet portrait, and mobile landscape resolutions.
- A normal clue must explicitly make its shared Close button visible. It must not depend on a previous slideshow's state.
- A regular clue must deactivate any active slideshow under the shared clue root before it takes control of the shared Close button.
- Close-button status text must remain visible while a multiplayer close gate is waiting.
- Room-list prefabs must be laid out so every visible room can be selected. The current scene's room-list content has no layout component, so room entries can overlap; this remains a known UI issue to resolve.

### 4.4 Analytics and email delivery

- CSV output includes individual player input activity and team submissions, attempts, correct/wrong counts, expected answers, and time taken.
- Only the Photon room organiser may send the email attachment.
- Credentials, app passwords, and API secrets must never be committed, displayed in a UI, placed in Photon properties, or included in logs.
- The intended production route is a server-side HTTPS endpoint. The repository currently has a separate .NET email API, but Unity does not yet call it; production delivery should consolidate on the backend route.

## 5. Non-functional requirements

- **Consistency:** all clients eventually render the same room stage and shared puzzle result.
- **Resilience:** a disconnect, rejoin, or master-client handoff must not strand the team in a puzzle lock, stage delay, or close gate.
- **Privacy and security:** do not publish room passwords or SMTP/API credentials beyond their necessary scope. Treat gameplay CSVs as potentially sensitive learning data.
- **Maintainability:** new puzzles must be registered in `PuzzleStateManager`, configured in `GameScene`, and verified in both single-player and two-client Photon runs.
- **Performance:** world interaction, UI updates, and Photon property/RPC traffic must remain responsive at the room's configured capacity.

## 6. Acceptance scenarios

### Disconnect during a puzzle

1. Player A owns the current puzzle lock.
2. Player A disconnects before completing the puzzle.
3. The master reconciles the lock; an active team member can open and submit the puzzle.
4. The team can advance without Player A returning.

### Disconnect during delayed stage advance

1. A puzzle is solved and a delayed stage transition is scheduled.
2. The player that solved it disconnects, including the possibility that they were the master.
3. The pending stage and Photon-time deadline remain in room properties.
4. The current master performs the transition once the deadline passes.

### Multiplayer clue close

1. Two or more active players open a clue.
2. A player clicks Close.
3. Their button remains on screen as `Waiting for (N players to close)` and cannot be clicked again.
4. The clue stays open until every active player votes.
5. When the final active player votes, the clue closes for all clients.

### Normal clue after a slideshow

1. A comic/slideshow was previously active.
2. A regular clue such as `Phone Message From Father` opens.
3. The regular clue owns the shared Close button; the button is visible and the clue's multiplayer close gate is used.

## 7. Current product risks and follow-up work

- Room passwords are published as Photon lobby custom properties so clients can validate them locally. This is convenience, not secure access control.
- The repository contains SMTP handling and credential material in backend source. Rotate any exposed credentials, move configuration into secure deployment secrets, and remove source-controlled values before release.
- The .NET CSV email API and `SmtpEmailSettings` asset are present, but the Unity logger currently sends SMTP directly instead of posting to that API. The two paths should be reconciled.
- There are no project-owned automated gameplay tests in the repository. Multiplayer recovery, stage transitions, and close gates need repeatable two-client verification.
