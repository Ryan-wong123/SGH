# SGH Architecture

## Overview

SGH is a Unity 6.3 URP game with Photon PUN/Realtimes services for room-based multiplayer. It has two enabled scenes:

```text
Lobby -> Photon room -> GameScene -> puzzle/story progression -> CSV output / exit
```

The repository also includes `backend/SghCsvEmailApi`, a standalone ASP.NET Core (.NET 10) service intended to accept a CSV and email it. It is not currently used by the Unity logger at runtime.

## Runtime topology

```text
Unity client A ----\                         /---- Unity client B
                   \-- Photon room/state ---/
                         |
                         +-- master-client authority
                         |     - scene load
                         |     - timer start/warnings
                         |     - puzzle lock repair
                         |     - pending stage advance
                         |
                         +-- gameplay event aggregation (master writes CSV)

Session organiser client -- direct SMTP (current implementation)

ASP.NET email API -- SMTP (present but not called by Unity today)
```

## Project layout

| Area | Responsibility |
| --- | --- |
| `Assets/Scenes/Lobby.unity` | Connection, character selection, room create/join/list, waiting-room UI. |
| `Assets/Scenes/GameScene.unity` | World, player spawn points, puzzle panels/triggers, Canvas UI, clue/comic panels, story objects. |
| `Assets/Scripts/networking` | Photon connection, room lifecycle, chat, session organiser metadata. |
| `Assets/Scripts/Player` | First-person controller, crosshair interaction, inventory/clues, player tags, objectives. |
| `Assets/Scripts/Interactable` and `Assets/Scripts/Keypad` | World interaction contracts and implementations. |
| `Assets/Scripts/Puzzle` | Stage orchestration, analytics, timers, sync helpers, and individual puzzle managers. |
| `Assets/Scripts/UI` | Sidebar, clue/comic UI, inventory, loading, date, and popup presentation. |
| `Assets/Resources` | Photon-instantiated character prefabs and runtime configuration assets. |
| `backend/SghCsvEmailApi` | Optional independent CSV email endpoint. |

Third-party Photon PUN, Photon Chat, TextMesh Pro, LeanTween, and Unity package code under `Assets/Photon`, `Assets/TextMesh Pro`, and `Assets/LeanTween` is vendor code; project behavior should be changed in `Assets/Scripts` and scene/prefab configuration.

## Scene and session flow

```text
Lobby
  Network
    -> ConnectUsingSettings / JoinLobby
  CharacterSelection
    -> player custom property: CharacterIndex
  CreateRoomManager / JoinRoomManager
    -> Photon room with RoomPassword, rejoin TTLs, organiser actor ID
  master StartGame
    -> PhotonNetwork.LoadLevel("GameScene")

GameScene
  GameManager
    -> Photon instantiate chosen player prefab
  PlayerController + CrosshairInteractor
    -> IInteractable world actions
  PuzzleTrigger
    -> PuzzleStateManager lock/attempt flow
  PuzzleStateManager
    -> puzzle manager, stage advance, reward/story/timer state
```

`PhotonNetwork.AutomaticallySyncScene` is enabled. The master client calls `PhotonNetwork.LoadLevel("GameScene")`; every room participant follows.

## Multiplayer state ownership

### Photon room properties

`PuzzleStateManager` owns the main puzzle state contract:

| Property | Meaning |
| --- | --- |
| `PuzzleLocked` | Whether the current puzzle attempt is locked. |
| `LockOwner` | Actor number owning the active puzzle attempt. |
| `Round` | Fair-turn round number. |
| `PendingAdvanceStage` | Solved stage awaiting a durable delayed transition; `-1` means none. |
| `PendingAdvanceAt` | Photon network time when that advance may execute. |

Each player has a `PlayedRound` property. Only non-inactive players are expected to take a turn. When everyone active has played, the master advances the round, clears the lock, and makes a new turn available.

`ClueUI` and `SlideShowUI` construct per-clue/per-comic room and player property keys for close votes. The keys include a round, a done flag, each player's vote, and a remaining-player count.

`CreateRoomManager` stores a `RoomPassword` property and the room is configured to expose it to the lobby. This makes client-side password checking possible but is not security-grade access control.

### RPCs and events

- Buffered RPCs are used for durable presentation/state effects that late joiners need to receive, including stage setting, shared inventory/clue additions, and selected world object changes.
- Non-buffered RPCs are used for transient synchronized UI and puzzle actions.
- Photon custom events are used for gameplay CSV aggregation (`41`) and clue remaining-player display updates (`42`). Do not reuse these event codes.
- Photon room/player property changes are the recovery mechanism for state that must survive a sender disconnect or master switch.

### Master-client duties

The master client is authoritative for:

- starting synchronized scenes;
- starting the shared Photon-time countdown and issuing warning RPCs;
- creating close-gate rounds;
- processing a pending puzzle stage transition when its `PendingAdvanceAt` time arrives;
- repairing/releasing/reassigning puzzle locks after membership changes;
- aggregating networked analytics and writing the gameplay CSV.

The master is replaceable. Any new master must be able to derive its next action from room properties, not from a coroutine owned only by a departed client. This is why delayed progression uses `PendingAdvanceStage` and `PendingAdvanceAt` rather than relying only on a local `WaitForSeconds` coroutine.

## Puzzle architecture

```text
PuzzleTrigger (world interaction)
  -> PuzzleStateManager.TryBeginAttempt()
      -> fair-turn and active-lock check
      -> submit ownership / local re-entry state
  -> panel activation

Puzzle UI submit
  -> PuzzleStateManager.Submit()
      -> stage-specific manager evaluates solution
      -> GameSessionCsvLogger records answer
      -> solved: persist delayed advance state
      -> current master executes advance at Photon time
      -> RPC_SetStageForAll / stage completion hooks
      -> story directive, clue reward, objective, timer reset
```

`PuzzleStateManager` is a scene singleton made persistent with `DontDestroyOnLoad`. It holds the stage-to-panel and stage-to-`IPuzzle` mappings. It is the integration point for a new puzzle; adding a manager without registering it here will not add it to the shared game flow.

Individual managers own local puzzle rules and UI. Shared input/feedback behavior is supplied by components such as `DraggableCard`, `DropSlot`, `PhotonSyncedUIButtonColor`, `PhotonSyncedTMPDropdown`, `PhotonSyncedInputField`, and `WordSearchManager`.

`GameTimerManager` computes time from `PhotonNetwork.Time`, not local elapsed time. The master starts/resets it and broadcasts the absolute end time so every client displays the same countdown.

`StorylineDirector` observes stage changes and applies configured scene effects: enable/disable GameObjects and components, queue/status text changes, audio, popups, and UnityEvents. It can be master-controlled and buffers stage RPCs for late joiners.

## World interaction architecture

```text
PlayerController owns local camera/movement
  -> CrosshairInteractor raycasts from player camera
      -> IInteractable.GetInteractionText / Interact
      -> optional IHighlightable.ShowHighlight / HideHighlight

Interactables:
  PuzzleTrigger, PickupItem, DoorInteractable, MovementTargetForward, KeypadButton
```

Door/keypad, pickup, and movement interactions use Photon views/RPCs when connected. The player prefab is created through `PhotonNetwork.Instantiate`, while scene-created objects can register their Photon view at runtime.

## Clue and comic UI architecture

`GameScene` contains one shared clue root and shared Close button. Multiple regular clue panels and slideshow panels live below that root.

```text
ClueSystem -> ClueUI.ShowById(id)
  -> deactivate active slideshow owners
  -> show selected regular clue and shared Close button
  -> start/read a shared close-vote round

Player clicks Close
  -> record player vote in Photon properties
  -> show "Waiting for (N players to close)"
  -> when all active players voted, set Done and hide for everyone
```

`SlideShowUI` owns comic navigation and uses the same button only while its slideshow is active. `ClueUI` deliberately releases any active slideshow before showing a normal clue so the close handlers do not compete. Both systems exclude `Player.IsInactive` from required close votes.

## Analytics and delivery

`GameSessionCsvLogger` is created automatically after scene load and persists across scenes. It gathers per-player entry/input events and team answer events through Photon event `41`. The master writes a CSV to the user's Downloads folder.

The active delivery code is direct SMTP in Unity and only runs for the room organiser (stored as an actor number in room custom properties). `SessionSmtpCredentials` holds sender/recipient/app-password values only in runtime memory.

`backend/SghCsvEmailApi` exposes:

| Endpoint | Behavior |
| --- | --- |
| `GET /health` | Returns `{ status: "ok" }`. |
| `POST /api/gameplay-csv` | Accepts multipart `csv`, `roomName`, and `playedAt`; rejects non-CSV or files larger than 5 MB; has a fixed-window 20 requests/minute limit. |

The backend currently sends to a fixed configured recipient and reads SMTP server settings through `GmailOptions`; its source should be migrated to deployment secrets. The Unity `SmtpEmailSettings` ScriptableObject validates a backend URL but has no current caller, so it does not route CSV delivery.

## Configuration and build

- Unity editor version: `6000.3.11f1`.
- Rendering: Universal Render Pipeline 17.3.0.
- Input: Unity Input System 1.19.0 plus project input configuration.
- Networking: Photon PUN/Realtime and Photon Chat packages stored in `Assets/Photon`.
- Enabled build scenes: `Assets/Scenes/Lobby.unity`, `Assets/Scenes/GameScene.unity`.
- .NET backend target: `net10.0`.

## Architectural invariants

1. Do not make forward progress depend solely on a local player's coroutine or object ownership. Persist recoverable state in Photon room properties.
2. A player with `IsInactive == true` must not block puzzle turns or close voting.
3. Preserve the master/client split: master decides shared transitions; all clients render shared results.
4. Do not let two UI controllers own the shared clue Close button at once.
5. Preserve current scene and inspector references when changing scripts. Unity serializes references by field name/type and scene object IDs.
6. Do not add secrets, SMTP credentials, or real production URLs to scene assets, source, logs, or docs.
