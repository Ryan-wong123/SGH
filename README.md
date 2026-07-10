# SGH Unity Project

This README documents the project-owned scripts under `Assets/Scripts`.
Third-party package scripts, Photon demo scripts, LeanTween, TextMesh Pro, and Unity tutorial helper files are not included.

Inspector fields listed here are the public fields and `[SerializeField]` fields that can be assigned or tweaked from the Unity Inspector. Runtime-only singletons, computed properties, and methods are not listed as tweakable fields.

## How The Scripts Work Together

The project is built around a Photon multiplayer flow, a local first-person interaction loop, and a puzzle-stage controller. In broad terms, players join a Photon room, `GameManager` spawns the selected character, `PlayerController` and `CrossHairInteractor` let the player move around and interact with world objects, and `PuzzleStateManager` decides which puzzle is currently available, when it opens, when it resets, and what happens after completion.

### Startup, Lobby, And Player Spawn

`Network` owns the shared Photon connection setup. `CreateRoomManager` uses that base behavior to create rooms, while `JoinRoomManager` lists available rooms, handles password entry, displays joined players, and starts the game scene. `CharacterSelection` saves the selected character index before gameplay begins. When a player joins the gameplay room, `GameManager` reads that selection, spawns the matching prefab at one of the configured spawn points, handles reconnect/leave callbacks, and provides restart/quit helpers. `LoadingScreenManager` is used by scene-transition UI so loading progress and tips can be shown while moving between scenes.

`ChatManager` connects Photon Chat to the active room after the player joins, sends messages from the chat input, receives room messages, and updates the unread badge when the chat panel is collapsed.

### Player Interaction Loop

The spawned player prefab uses `PlayerController` for movement, camera look, camera reset, and cursor state. `PlayerTagDisplay` renders player names above networked players. `CrossHairInteractor` raycasts from the player camera, finds objects that implement `IInteractable`, shows the prompt returned by the target, and calls `Interact()` when the player presses the interaction key. If the target also implements `IHighlightable`, `CrossHairInteractor` turns highlighting on and off while the player looks at it.

`HighlightEffect` is the common highlight implementation used by interactable objects. `PickupItem`, `PuzzlePanelTrigger`, and `MovementTargetNetworked` all participate in this interaction flow. `DoorInteractable` is also interactable and can be opened directly, by key, or through `Keypad`. `WaypointTrigger` and `ObjectivePointer` work together for guidance: triggers advance configured waypoint paths, and the pointer aims the UI arrow toward the next active target. `ClosePanelAndResetCamera` restores the camera and cursor after closing older UI panels.

### Inventory, Clues, And Popups

`InventorySystem` stores collected inventory item names at runtime, and `InventoryUIManager` rebuilds the visible inventory list from that data. If a pickup is marked as a clue instead of an item, `PickupItem` sends it to `ClueSystem`, which tracks clue reveal state and updates clue buttons/icons. `ClueUI` displays clue panels, image details, unread badges, and optional synchronized close voting across players. `CluePopupUI` shows short notification popups when clues or story events need to notify the player.

`SideBarMenuManager` controls sidebar panels such as inventory, clue, settings, or instruction popups so only one is open at a time. `DynamicDateText` and the date entries inside puzzle/story scripts keep date labels relative to the current date. `SlideShowUI` displays comic/slideshow sequences and can optionally advance the puzzle stage or leave the room when closed. `TriggerComic` is a simpler scene helper for activating comic-related objects or swapping text.

### Puzzle Stage Flow

`PuzzleStateManager` is the main coordinator for puzzle progression. `PuzzlePanelTrigger` calls into it when a player starts a puzzle. The manager opens the panel for the current `PuzzleStage`, starts or resets timer state, delegates answer checking to the relevant puzzle manager, advances to the next stage when solved, shows clue rewards, and closes panels when needed. `GameTimerManager` supplies the countdown timer and calls puzzle reload logic when time expires. `PuzzleScoreTracker` records wrong attempts and can show the final score panel. `StorylineDirector` watches the current puzzle stage and applies scene changes such as activating objects, disabling components, updating `QueueNumberDisplay`, changing status text, playing audio, firing `UnityEvent` hooks, and showing `CluePopupUI` messages.

Most puzzle managers implement `IPuzzle`, which gives `PuzzleStateManager` a common way to check solved state and reset puzzle state. `PuzzleFeedbackSyncExtensions`, `PhotonSyncedUIButtonColor`, `PhotonSyncedTMPDropdown`, and `PhotonSyncedInputField` keep answer feedback colors and selected UI state synchronized between Photon clients.

### Drag-And-Drop Puzzle Stack

Drag-card puzzles share `DraggableCard` and `DropSlot`. `DraggableCard` handles pointer drag events, ownership requests, networked parent/position/color syncing, card flipping, card metadata, and optional drag-name tags. `DropSlot` accepts cards, snaps them into position, tracks all active slots, and provides base correct/wrong feedback colors. Activity-specific drop slots override `IsCardCorrect()` to validate puzzle-specific metadata:

- `Activity1DropSlot` checks symptom names for `SymptomsCardManager`.
- `Activity1_2DropSlot` checks configured answers for `BearCardManager`.
- `Activity2DropSlot` checks `stepName` for activity 4.
- `Activity6DropSlot` checks insulin-related `stepName` values for activity 8.
- `Activity9DropSlot` checks death-lapses answers and can link stage-two slots to stage-one answers.

`InputClickButton` is a small generic click-to-event helper used where UI pointer input needs to trigger a configured `UnityEvent`. `CardPopupManager` shows a larger preview image for selected cards.

### Individual Puzzle Managers

Each activity manager owns one puzzle's rules and reports its result back to `PuzzleStateManager`.

| Puzzle script | Works with | Role in the flow |
| --- | --- | --- |
| `SymptomsCardManager` | `Activity1DropSlot`, `DraggableCard`, `DropSlot` | Checks whether all required symptom cards are placed in correct slots. |
| `BearCardManager` | `Activity1_2DropSlot`, `DraggableCard`, `DropSlot` | Checks whether enough BEAR cards are placed correctly. |
| `PictureGridManager` | `PhotonSyncedUIButtonColor` | Tracks selected picture buttons, checks selected indexes, and colors correct/wrong choices. |
| `Activity2PuzzleManager` | `Activity2DropSlot`, input fields | Manages a multi-part puzzle with draggable-card and typed-input sections. |
| `CrosswordManager` | `CrosswordCell` | Builds the crossword grid, reads cell letters, and checks the completed crossword. |
| `DiaryPuzzleManager` | TMP input fields, date entries | Checks staged diary answers and controls staged diary panels. |
| `PrescriptionManager` | TMP input field, date entries | Checks the prescription amount and updates date labels. |
| `InsulinPuzzleManager` | `Activity6DropSlot`, TMP dropdown | Checks the insulin dropdown and draggable-card sections. |
| `WordSearchManager` | `LetterCell` | Builds the word-search grid, handles drag selection, syncs cells, and validates the selected answer. |
| `RiskTable` | TMP dropdowns | Checks dropdown selections and applies feedback colors. |
| `DeathLapsesManager` | `Activity9DropSlot` | Controls the two-stage death-lapses drag puzzle. |
| `PuzzleTileManager` | `PuzzleTile` | Checks whether every rotating tile is at the correct rotation. |

`CrosswordCell`, `LetterCell`, and `PuzzleTile` are per-cell/per-tile helper components controlled by their matching manager rather than by `PuzzleStateManager` directly.

### Keypad And Doors

`KeypadButton` sends a configured value into `Keypad` and animates the pressed button. `Keypad` builds the entered code, updates the keypad display and screen colors, plays sound effects, compares the input to `keypadCombo`, and unlocks each configured `DoorInteractable` when the code is correct. `DoorInteractable` then handles the actual door rotation, lock/key requirement, and optional auto-open behavior.

### Logging And End Of Game

`GameSessionCsvLogger` listens for puzzle events, records puzzle entries, submitted answer snapshots, correct/wrong counts, and time spent, then saves a CSV file when the game ends or the room flow exits. `PuzzleScoreTracker` separately tracks wrong attempt counts for the in-game end screen. `QuitButton` and `GameManager.QuitGame()` use the same quit path so the session data can be saved before the app exits.

### Editor Helper

`DateFormatAttribute` and its editor drawer customize how date-format fields appear in the Unity Inspector. Scripts such as `DynamicDateText`, `StorylineDirector`, `DiaryPuzzleManager`, and `PrescriptionManager` use date-format/date-offset fields so scene text can stay relative to the current date without hardcoding a fixed calendar value.

## Script Reference

### Core Gameplay

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/GameManager.cs` | Handles Photon room callbacks, player spawning, scene restart, leaving rooms, and quitting the game. | `characterPrefabs`, `spawnPoints` |
| `Assets/Scripts/GameTimeManager.cs` | Runs the networked puzzle countdown timer and displays timed warning messages. | `countdownDuration`, `timerText`, `timeWarningPanel`, `warningText`, `warnings`; each warning exposes `thresholdSeconds`, `message`, `color`, `slideDuration` |
| `Assets/Scripts/CharacterSelection.cs` | Stores the selected player character and updates character selection button colors. | `characterButtons`, `selectedColor`, `normalColor` |
| `Assets/Scripts/QueueNumberDisplay.cs` | Updates a world-space queue display and can flash/pulse its text or material emission. | `label`, `displayText`, `isFlash`, `flashSpeed`, `alphaRange`, `cubeRenderer`, `pulseEmission`, `emissionSpeed`, `emissionRange`, `emissionColor`, `baseTextColor` |
| `Assets/Scripts/StorylineDirector.cs` | Watches puzzle stage changes and applies configured story effects such as activating objects, updating text, playing audio, and showing popups. | `puzzleStateManager`, `cluePopupUI`, `masterControls`, `pollIntervalSeconds`, `bufferStageRpcForLateJoiners`, `playEnterAudioOnInitialApply`, `showPopupMessagesOnInitialApply`, `directives`; each directive exposes `stage`, object lists, queue/status/popup directives, audio settings, and `onEnter` |
| `Assets/Scripts/TriggerComic.cs` | Activates configured objects and optionally changes a TMP text value when triggered. | `objectsToActivate`, `textUI`, `newText` |
| `Assets/Scripts/QuitButton.cs` | Small UI helper that calls the game quit flow. | None |

### Networking And Lobby

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/networking/Network.cs` | Base Photon connection/lobby flow, username setup, room rejoin cache, and shared network callbacks. | `gameVersion`, `maxPlayersPerRoom`, `homePanel`, `usernameInput`, `goToRoomListButton`, `joinRoomManager` |
| `Assets/Scripts/networking/CreateRoomManager.cs` | Handles room creation UI and creates Photon rooms with the selected room name and player limit. | `roomNameInput`, `maxPlayersDropdown`, `createRoomButton`, `togglecreateRoomPanelButton`, `createRoomPanel` |
| `Assets/Scripts/networking/JoinRoomManager.cs` | Builds the room list UI, handles password-gated joins, displays room/player info, and starts the game for the master client. | `roomItemPrefab`, `contentPanel`, `refreshButton`, `toggleRoomListPanelButton`, `roomListPanel`, `roomPanel`, `playerCountText`, `playerListContent`, `playerListItemPrefab`, `startGameButton`, `loadingScreen`, `roomPasswordDisplayText`, `selectedRoomLabelText`, `joinRoomFeedbackText`, `roomPasswordInputField`, `confirmJoinRoomButton` |
| `Assets/Scripts/networking/ChatManager.cs` | Connects Photon Chat to the current room, sends/receives messages, and manages unread chat UI. | `inputField`, `sendButton`, `chatContent`, `chatMessagePrefab`, `channelDropdown`, `chatAppId`, `chatPanel`, `messageTextColor`, `unreadBadge`, `unreadText`, `collapseButton` |

### UI

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/UI/SlideShowUI.cs` | Displays comic/slideshow panels with navigation, optional autoplay, optional synchronized close voting, audio, and stage/lobby actions on close. | `displayImage`, `prevButton`, `nextButton`, `closeButton`, `requireAllPlayersToClickClose`, `comicId`, `advancePuzzleStageOnClose`, `closeTargetStage`, `leaveRoomAndLoadLobbyOnClose`, `lobbySceneName`, `slides`, `audioSource`, `onEnableClip`, `autoPlay`, `loop`, `defaultSlideDuration`, `closeButtonTextPadding`, `closeButtonMaxWidth`, `portraitSize`, `landscapeSize`, `preserveAspect`; each slide exposes `image`, `duration`, `orientationMode` |
| `Assets/Scripts/UI/SideBarMenuManager.cs` | Controls sidebar buttons, popup panels, and a blocker object so only one popup is open at a time. | `sidebarPopups`, `popupBlocker`; each popup exposes `button`, `panel` |
| `Assets/Scripts/UI/LoadingScreenManager.cs` | Shows a loading panel, updates a progress bar, randomizes loading tips, and fades while loading scenes. | `loadingPanel`, `progressBarFill`, `tipText`, `fadeDuration`, `loadingTips` |
| `Assets/Scripts/UI/InventoryUIManager.cs` | Rebuilds the visible inventory list from `InventorySystem` and toggles the inventory UI. | `contentParent`, `itemPrefab` |
| `Assets/Scripts/UI/DynamicDateText.cs` | Writes a date into a TMP text field using an offset from today and a configurable format. | `dateText`, `daysBack`, `format` |
| `Assets/Scripts/UI/ClueUI.cs` | Manages clue panels, unread clue badges, full-size clue images, and optional synchronized close voting. | `mainRoot`, `clues`, `closeButton`, `unreadBadgeRoot`, `unreadText`, `badgePanelRoot`, `detailPanelRoot`, `detailImage`, `detailCloseButton`, `comics`, `requireAllPlayersToClickClose`, `clueCloseId`, `closeButtonTextPadding`, `closeButtonMaxWidth`, `portraitSize`, `landscapeSize`; each clue exposes `id`, `panelRoot` |
| `Assets/Scripts/UI/CluePopupUI.cs` | Queues and animates short clue notification popups. | `panel`, `message`, `fadeInDuration`, `holdDuration`, `fadeOutDuration`, `slidePixels` |

### Player

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Player/PlayerController.cs` | Handles local player movement, mouse look, camera ownership, and cursor locking. | `moveSpeed`, `mouseSensitivity`, `playerCamera`, `cameraRoot`, `verticalRotation` |
| `Assets/Scripts/Player/CrossHairInteractor.cs` | Raycasts from the player camera, shows interaction prompts, highlights targets, and calls interactable objects. | `playerCamera`, `interactDistance`, `interactableLayer`, `interactionUIPanel`, `preferKeypadButtons` |
| `Assets/Scripts/Player/PlayerTagDisplay.cs` | Displays player name tags and can hide the tag for the local owner. | `fontSize`, `textColor`, `hideForLocalOwner` |
| `Assets/Scripts/Player/ObjectivePointer.cs` | Points the local player toward configured waypoint paths for the current objective. | `player`, `arrowObject`, `paths`, `heightOffset`, `forwardOffset`; each path exposes `id`, `waypoints` |
| `Assets/Scripts/Player/WaypointTrigger.cs` | Advances or changes the active waypoint objective when the player enters a trigger. | `pathId`, `waypointIndex`, `playerTag` |
| `Assets/Scripts/Player/ClosePanelAndResetCamera.cs` | Close-button helper that restores the local camera view and gameplay cursor lock after closing legacy panels. | None |
| `Assets/Scripts/Player/InventorySystem.cs` | Runtime singleton that stores inventory items. | None |
| `Assets/Scripts/Player/ClueSystem.cs` | Stores clue reveal state and updates clue icon/button UI. | clue entries expose `id`, `button`, `icon`, `lockedSprite`, `revealedSprite`, `fullImage`; `gridParent` |

### Interactable Objects

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Interactable/IInteractable.cs` | Interface for objects that can be interacted with. | None |
| `Assets/Scripts/Interactable/IHighlightable.cs` | Interface for objects that can show and hide highlight effects. | None |
| `Assets/Scripts/Interactable/HighlightEffect.cs` | Applies material emission highlighting and optional scale animation to renderers. | `highlightColor`, `emissionIntensity`, `highlightScaleMultiplier`, `scaleSpeed`, `enableScaling`, `useWhitelist`, `allowedChildren`, `excludedChildren` |
| `Assets/Scripts/Interactable/PuzzlePanelTrigger.cs` | Starts a puzzle interaction, optionally shows a clue first, blocks completed puzzles, and manages puzzle UI close state. | `puzzleStateManager`, `clueUI`, `clueId`, `startWithClue`, `dontDestroyOnCompletion`, `alreadyPlayedImage`, `warningMessage`, `showDuration` |
| `Assets/Scripts/Interactable/PickupItem.cs` | Adds an item or clue to the player systems, shows optional notifications, and syncs pickup behavior over Photon. | `itemName`, `isClue`, `notificationMessage`, `sharedDisplayId` |
| `Assets/Scripts/Interactable/DoorInteractable.cs` | Opens/closes doors, supports locked/key-required doors, and can unlock/open via keypad or key. | `hinge`, `openAngle`, `smooth`, `smoothSpeed`, `isLeftDoor`, `requiresKey`, `keyName`, `isLocked`, `openOnUnlock`, `permanentlyDisableKeyRequirementOnUnlock` |
| `Assets/Scripts/Interactable/MovementTargetNetworked.cs` | Moves a networked object forward once and syncs its position/movement state over Photon. | `moveDistance`, `moveSpeed` |

### Keypad

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Keypad/Keypad.cs` | Handles keypad input, validates a numeric code, updates display/emission feedback, plays sounds, and unlocks doors. | `keypadCombo`, `accessGrantedText`, `accessDeniedText`, `displayResultTime`, `screenIntensity`, `screenNormalColor`, `screenDeniedColor`, `screenGrantedColor`, `buttonClickedSfx`, `accessDeniedSfx`, `accessGrantedSfx`, `panelMesh`, `keypadDisplayText`, `audioSource`, `doorsToUnlock`, `keyName` |
| `Assets/Scripts/Keypad/KeypadButton.cs` | Sends a keypad button value to `Keypad` and animates the physical button press. | `keypad`, `value`, `bttnspeed`, `buttonPressedTime`, `moveDist` |

### Puzzle Framework

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Puzzle/IPuzzle.cs` | Interface implemented by puzzle managers that can check solved state and reset. | None |
| `Assets/Scripts/Puzzle/PuzzleStateManager.cs` | Central networked puzzle flow controller. Opens/closes puzzle panels, tracks the current stage, handles timers, completion transitions, clue rewards, and submit ownership. | puzzle object/panel references for each stage, each puzzle manager reference, `delayTransition`, `movementTargetPuzzle11`, `puzzleClueRewards`; each clue reward exposes `stage`, `clueId`, `popupMessage` |
| `Assets/Scripts/Puzzle/PuzzleScoreTracker.cs` | Tracks wrong attempts per puzzle through Photon room properties and can show an end-game summary panel. | `endScreenPanel`, `endStatsText` |
| `Assets/Scripts/Puzzle/GameSessionCsvLogger.cs` | Runtime logger that records per-player puzzle entries, attempts, answers, wrong/correct counts, and time spent, then saves CSV output. | None |
| `Assets/Scripts/Puzzle/PuzzleFeedbackSyncExtensions.cs` | Helper extension methods for syncing puzzle feedback visuals across supported UI components. | None |
| `Assets/Scripts/Puzzle/PhotonSyncedUIButtonColor.cs` | Syncs UI button selected/correct/wrong color state over Photon, with optional grouped selection behavior. | `selectedColor`, `normalColor`, `allowToggleOff`, `autoGroupByParent`, `groupId`, `isSelected`, `resultState` |
| `Assets/Scripts/Puzzle/PhotonSyncedTMPDropdown.cs` | Syncs TMP dropdown value and background color over Photon. | `dropdown` |
| `Assets/Scripts/Puzzle/PhotonSyncedInputField.cs` | Syncs TMP input field background color over Photon. | `input` |

### Drag-And-Drop Puzzle Helpers

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Puzzle/Draggable/DraggableCard.cs` | Makes a UI card draggable, handles card front/back visuals, slot placement, drag tags, locking, and optional panel routing. | `canvas`, `panel1GridLayout`, `panel2GridLayout`, `pathRoot`, `cardBehavior`, `symptomName`, `backSprite`, `stepName`, `lapsesAnswer`, `isLocked`, `dragTagSize`, `dragTagOffset`, `dragTagBackgroundColor`, `dragTagTextColor`, `dragTagFontSize`, `frontSprite`, `lastSlotOrParent` |
| `Assets/Scripts/Puzzle/Draggable/DropSlot.cs` | Base drop slot for draggable-card puzzles. Snaps dropped cards and provides correct/wrong coloring helpers. | `correctColor`, `wrongColor`, `positionSnap`, `scaleSnap` |
| `Assets/Scripts/Puzzle/Draggable/InputClickButton.cs` | Generic pointer-click helper that invokes a UnityEvent for the configured mouse button. | `triggerButton`, `onTriggered` |
| `Assets/Scripts/Puzzle/Draggable/CardPopupManager.cs` | Displays a popup preview image for cards. | `popupPanel`, `cardImageDisplay` |

### Puzzle Activities

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Puzzle/activity 1/SymptomsCardManager.cs` | Checks the symptom-card drag puzzle and reports completion when the required number of correct cards is reached. | `required` |
| `Assets/Scripts/Puzzle/activity 1/Activity1DropSlot.cs` | Drop slot variant for the symptom puzzle; validates cards by symptom name. | inherited `DropSlot` fields, `correctAnswers` |
| `Assets/Scripts/Puzzle/activity 2/BearCardManager.cs` | Checks the BEAR-card drag puzzle and reports completion when enough cards are correctly placed. | `required` |
| `Assets/Scripts/Puzzle/activity 2/Activity1_2DropSlot.cs` | Drop slot variant for the BEAR puzzle; validates cards by symptom/name. | inherited `DropSlot` fields, `correctAnswers` |
| `Assets/Scripts/Puzzle/activity 3/PictureGridManager.cs` | Manages the selectable picture grid puzzle, selected count text, answer checking, and submit button state. | `pictureButtons`, `correctIndexes`, `selectedColor`, `correctColor`, `wrongColor`, `defaultColor`, `selectedCountText`, `submitButton` |
| `Assets/Scripts/Puzzle/activity 4/Activity2PuzzleManager.cs` | Manages a multi-part activity with draggable slots and input fields, including part-two transitions. | `required`, `panel1`, `panel2`, `input1`, `input2` |
| `Assets/Scripts/Puzzle/activity 4/Activity2DropSlot.cs` | Drop slot variant for activity 4; validates dragged cards by configured step name and required correct count. | inherited `DropSlot` fields, `stepName`, `requiredCorrect` |
| `Assets/Scripts/Puzzle/activity 5/CrosswordManager.cs` | Builds and checks the crossword grid puzzle. | `cellPrefab`, `gridParent`, `gridSize` |
| `Assets/Scripts/Puzzle/activity 5/CrosswordCell.cs` | Represents one crossword cell and controls its input field, position, active state, and letter value. | `inputField`, `position`, `isActive` |
| `Assets/Scripts/Puzzle/activity 6/DiaryPuzzleManager.cs` | Manages the staged diary-input puzzle, accepted answers, date text entries, navigation, checking, and reset. | `answers`, `stagePanels`, `dateEntries`; each answer exposes `field`, `stage`, `caseSensitive`, `accepted`; each date entry exposes `dateText`, `daysBack` |
| `Assets/Scripts/Puzzle/activity 7/PrescriptionManager.cs` | Checks the prescription amount input and updates configured date text entries. | `prescriptionAmount`, `dateEntries`; each date entry exposes `dateText`, `daysBack` |
| `Assets/Scripts/Puzzle/activity 8/InsulinPuzzleManager.cs` | Manages the insulin puzzle panels, dropdown answer, draggable section, color feedback, and completion check. | `required`, `panel1`, `panel2`, `syringeDropdown`, `correctSyringeIndex`, `correctColor`, `wrongColor`, `defaultColor` |
| `Assets/Scripts/Puzzle/activity 8/Activity6DropSlot.cs` | Drop slot variant for activity 8; validates insulin-related dragged cards by step name and required correct count. | inherited `DropSlot` fields, `stepName`, `requiredCorrect` |
| `Assets/Scripts/Puzzle/activity 9/WordSearchManager.cs` | Builds and syncs the word-search grid, handles drag/toggle selection, validates answers, and colors rows/columns. | `gridPanel`, `cellPrefab`, `gridSize`, `useFixedPlacement`, `rowColor`, `ColumnColor` |
| `Assets/Scripts/Puzzle/activity 9/LetterCell.cs` | Represents one word-search cell, handles pointer drag events, selected state, result state, text, and background visuals. | `letterText`, `backgroundImage` |
| `Assets/Scripts/Puzzle/activity 10/RiskTable.cs` | Checks TMP dropdown answers in a risk table and colors dropdowns as correct or wrong. | `dropdown`, `correctIndex`, `correctColor`, `wrongColor`, `defaultColor` |
| `Assets/Scripts/Puzzle/activity 11/DeathLapsesManager.cs` | Manages the two-stage death-lapses drag puzzle and switches between answer panels/slots. | `answer2Cards`, `answer2Slots`, `requiredAnswer1`, `requiredAnswer2` |
| `Assets/Scripts/Puzzle/activity 11/Activity9DropSlot.cs` | Drop slot variant for activity 11; can link stage-two validation to a stage-one slot. | inherited `DropSlot` fields, `linkedStage1Slot`, `requiredCorrect` |

### Rotating Puzzle

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/Puzzle/Rotating Puzzle/PuzzleTile.cs` | Rotates one tile and checks whether its rotation matches the target rotation. | `correctRotation` |
| `Assets/Scripts/Puzzle/Rotating Puzzle/PuzzleTileManager.cs` | Checks all rotating puzzle tiles and resets them when needed. | `tiles` |

### Editor/Inspector Helpers

| Script | What it does | Inspector fields |
| --- | --- | --- |
| `Assets/Scripts/core/DateFormatAttribute.cs` | Defines a custom property attribute and editor drawer for date-format fields with optional preview labels. | Attribute constructor options: `showPreview`, `label` |

## Gameplay CSV Logging

Gameplay puzzle data is recorded by `Assets/Scripts/Puzzle/GameSessionCsvLogger.cs`.
The logger is created automatically at runtime, so no scene setup is required.

### When CSV Is Saved

The CSV is written when:

- The final puzzle is completed.
- The player leaves the room through the game flow.
- The player quits through `GameManager.QuitGame()`.

If the Unity Editor play session is stopped directly from the editor controls, that may not trigger the in-game quit flow.

### Where CSV Is Saved

CSV files are saved under:

```text
Application.persistentDataPath/GameplayLogs
```

On Windows in the Unity Editor, this is usually similar to:

```text
C:\Users\<username>\AppData\LocalLow\<CompanyName>\<ProductName>\GameplayLogs
```

The exact path is printed in the Unity Console when the file is saved:

```text
Gameplay CSV saved to: <full path>
```

### CSV Columns

Each row represents one player on one puzzle.

```csv
room_name,player_name,puzzle_number,times_entered,correct_answers,wrong_answers,answer_try_1,answer_try_2,time_taken
```

- `room_name`: Photon room name where the gameplay happened.
- `player_name`: Player username.
- `puzzle_number`: Puzzle stage number.
- `times_entered`: Number of times the player opened or re-entered that puzzle.
- `correct_answers`: Number of correct submissions for that puzzle.
- `wrong_answers`: Number of wrong submissions for that puzzle.
- `answer_try_1`, `answer_try_2`, etc.: The answer snapshot submitted on each try. The CSV creates as many try columns as needed for the highest number of submissions in that file.
- `time_taken`: Total time spent by that player on that puzzle, saved in minutes and seconds as `MM:SS`.

Answer try columns are captured on submit. They can include dragged cards in slots, dropdown choices, typed input, selected buttons/images, and selected word-search cells depending on the puzzle type. If a player submits the same answer multiple times, each submit is still saved in its own try column.

### How Wrong Answer Selections Are Recorded

When a submitted answer is wrong, `wrong_answers` increases by 1 and the next `answer_try_N` column stores the full answer snapshot submitted at that moment. The snapshot includes all submitted selections, not only the incorrect parts.

Different puzzle input types are saved like this:

- Card/drag puzzles: saved as the slot name and the card placed in that slot.

```text
SlotName=[CardName]
```

The card name is taken from the card metadata in this order: `symptomName`, `stepName`, `lapsesAnswer`, then the card GameObject name if no metadata is set.

- Text input puzzles: saved as the input field GameObject name and the submitted text.

```text
InputFieldName=SubmittedText
```

- Dropdown puzzles: saved as the dropdown GameObject name and the selected option text.

```text
DropdownName=SelectedOptionText
```

- Button/image selection puzzles, including Puzzle 3: saved as the selected button label. If the button has visible TMP text, that text is saved. Otherwise, the button GameObject name is saved.

```text
SelectedButton=ButtonLabelOrGameObjectName
```

Puzzle 3 does not currently save the numeric `pictureButtons` index. It saves the button label or GameObject name.

- Word-search puzzles: saved as selected grid coordinates and the selected letter.

```text
Cell(x,y)=Letter
```

### Time Tracking Behavior

Time is accumulated per player and puzzle.

If a puzzle resets because the timer expires, the time already spent before the reset is still added to `time_taken`. When the player continues after the reset, the new time is added to the same puzzle total.

If the CSV is saved while a player is still mid-puzzle, the active time so far is included before writing the file.

### Multiplayer Behavior

For multiplayer rooms, each client sends its puzzle events to the Master Client. The Master Client aggregates the data and writes the CSV file once.
