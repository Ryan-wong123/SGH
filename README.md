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
