# SGH Unity Project

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
room_name,player_name,puzzle_number,times_entered,correct_answers,wrong_answers,time_taken_minutes
```

- `room_name`: Photon room name where the gameplay happened.
- `player_name`: Player username.
- `puzzle_number`: Puzzle stage number.
- `times_entered`: Number of times the player opened or re-entered that puzzle.
- `correct_answers`: Number of correct submissions for that puzzle.
- `wrong_answers`: Number of wrong submissions for that puzzle.
- `time_taken_minutes`: Total time spent by that player on that puzzle, saved in minutes.

### Time Tracking Behavior

Time is accumulated per player and puzzle.

If a puzzle resets because the timer expires, the time already spent before the reset is still added to `time_taken_minutes`. When the player continues after the reset, the new time is added to the same puzzle total.

If the CSV is saved while a player is still mid-puzzle, the active time so far is included before writing the file.

### Multiplayer Behavior

For multiplayer rooms, each client sends its puzzle events to the Master Client. The Master Client aggregates the data and writes the CSV file once.
