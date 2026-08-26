using System.Collections.Generic; // Imports a namespace used by this script.
using System.Collections; // Imports a namespace used by this script.
using ExitGames.Client.Photon; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

public class WordSearchManager : MonoBehaviourPunCallbacks, IPuzzle, IOnEventCallback // Declares this type.
{ // Starts this code block.
    private const byte WordSearchCellStateEventCode = 42; // Declares a field, property, or member used by this script.
    private const string WordSearchCellStatePrefix = "P5WS_"; // Declares a field, property, or member used by this script.

    [Header("Grid Setup")] // Adds a labeled section in the Unity Inspector.
    public GameObject gridPanel; // Declares a field, property, or member used by this script.
    public GameObject cellPrefab; // Declares a field, property, or member used by this script.
    [Range(2, 40)] public int gridSize = 20; // includes header row/col

    [Header("Letters")] // Adds a labeled section in the Unity Inspector.
    [SerializeField] private bool useFixedPlacement = false; // kept for compatibility; placement is always applied below

    // Selection state
    private bool isDragging = false; // Declares a field, property, or member used by this script.
    private bool dragAddMode = true; // true: selecting, false: unselecting
    private readonly HashSet<LetterCell> selected = new(); // Declares a field, property, or member used by this script.
    private bool suppressNetworkStateApply; // Declares a field, property, or member used by this script.

    // Grid
    private LetterCell[,] grid; // Declares a field, property, or member used by this script.

    [SerializeField] private Color rowColor; // Exposes this private field in the Unity Inspector.
    [SerializeField] private Color ColumnColor; // Exposes this private field in the Unity Inspector.

    private bool _built = false; // Declares a field, property, or member used by this script.
    private static WordSearchManager _activeInstance; // Declares a field, property, or member used by this script.

    // Solution map: '\0' means "no specific correct letter for this cell"
    private char[,] solution = new char[21, 21] // Declares a field, property, or member used by this script.
    { // Starts this code block.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L','I','N','E','Z','O','L','I','D' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','R','E','D','U','C','T','I','O','N','F','\0','\0','\0','\0','U'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','L'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','U','\0','\0','\0','\0','F'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','O'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','N'  }, // Runs this line of puzzle logic.
        { '\0','A','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','Y'  }, // Runs this line of puzzle logic.
        { '\0','Z','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','Q','\0','\0','\0','\0','L'  }, // Runs this line of puzzle logic.
        { '\0','O','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','U','\0','\0','\0','\0','U'  }, // Runs this line of puzzle logic.
        { '\0','L','\0','\0','\0','\0','\0','E','\0','\0','\0','\0','\0','\0','\0','I','\0','\0','\0','\0','R'  }, // Runs this line of puzzle logic.
        { '\0','E','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','\0','\0','\0','N','\0','\0','\0','\0','E'  }, // Runs this line of puzzle logic.
        { '\0','S','\0','\0','\0','\0','\0','D','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','A'  }, // Runs this line of puzzle logic.
        { '\0','I','M','P','A','I','R','E','D','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','S'  }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','\0','\0','\0','N','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','Y','\0','\0','\0','\0','\0','\0','\0','E','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' }, // Runs this line of puzzle logic.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' } // Runs this line of puzzle logic.
    }; // Runs this line of puzzle logic.

    private void Awake() // Declares a method used by this script.
    { // Starts this code block.
        // HARD BLOCK: only 1 active WordSearchManager at runtime
        if (_activeInstance != null && _activeInstance != this) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.LogWarning($"[WordSearch] Duplicate WordSearchManager detected -> DISABLING this one on {gameObject.name}"); // Writes a warning message to the Unity console.
            gameObject.SetActive(false); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.
        _activeInstance = this; // Updates this value.
    } // Ends this code block.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        PhotonNetwork.AddCallbackTarget(this); // Runs this line of puzzle logic.

        if (_built) // Checks this condition before continuing.
            ApplyRoomSyncedCellStates(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDisable() // Declares a method used by this script.
    { // Starts this code block.
        PhotonNetwork.RemoveCallbackTarget(this); // Runs this line of puzzle logic.

        if (_activeInstance == this && !gameObject.activeInHierarchy) // Checks this condition before continuing.
            _activeInstance = null; // Updates this value.
    } // Ends this code block.

    private void Start() // Declares a method used by this script.
    { // Starts this code block.
        // If something calls Start twice / object re-enabled, don’t build again
        if (_built) return; // Checks this condition before continuing.

        // Always start clean (prevents any leftover children if prefab already contains them)
        ClearGridPanelImmediate(); // Runs this line of puzzle logic.

        GenerateGrid(); // Runs this line of puzzle logic.
        PlaceFixedLettersFromSolution(); // Runs this line of puzzle logic.
        FillRandomLetters(); // Runs this line of puzzle logic.
        ApplyRoomSyncedCellStates(); // Runs this line of puzzle logic.

        _built = true; // Updates this value.

        Debug.Log($"[WordSearch] BUILT on '{gridPanel.name}' path='{GetTransformPath(gridPanel.transform)}' " + // Writes a diagnostic message to the Unity console.
                  $"children={gridPanel.transform.childCount} expected={gridSize * gridSize}"); // Runs this line of puzzle logic.

        // GLOBAL proof: if you see a 2nd grid, this number will be > expected overall
        int totalCells = FindObjectsOfType<LetterCell>(true).Length; // Creates a local value used by the following logic.
        Debug.Log($"[WordSearch] TOTAL LetterCell in scene (all parents) = {totalCells}"); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.
    private void ClearGridPanelImmediate() // Declares a method used by this script.
    { // Starts this code block.
        if (gridPanel == null) return; // Checks this condition before continuing.

        // runtime-safe: Destroy existing children (if any)
        for (int i = gridPanel.transform.childCount - 1; i >= 0; i--) // Repeats this block for a counted loop.
        { // Starts this code block.
            Destroy(gridPanel.transform.GetChild(i).gameObject); // Runs this line of puzzle logic.
        } // Ends this code block.
    } // Ends this code block.
    private static string GetTransformPath(Transform t) // Declares a method used by this script.
    { // Starts this code block.
        var stack = new Stack<string>(); // Creates a local value used by the following logic.
        while (t != null) // Repeats this block while the condition stays true.
        { // Starts this code block.
            stack.Push(t.name); // Runs this line of puzzle logic.
            t = t.parent; // Updates this value.
        } // Ends this code block.
        return string.Join("/", stack); // Returns this result to the caller.
    } // Ends this code block.
    private void GenerateGrid() // Declares a method used by this script.
    { // Starts this code block.
        grid = new LetterCell[gridSize, gridSize]; // Updates this value.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                var obj = Instantiate(cellPrefab, gridPanel.transform); // Creates a local value used by the following logic.
                var cell = obj.GetComponent<LetterCell>(); // Creates a local value used by the following logic.
                cell.Init(this, new Vector2Int(x, y)); // Runs this line of puzzle logic.

                // Top-left corner
                if (x == 0 && y == 0) // Checks this condition before continuing.
                { // Starts this code block.
                    cell.SetLetter(' '); // Runs this line of puzzle logic.
                    cell.DisableCellUI(); // Runs this line of puzzle logic.
                    cell.SetTextColor(Color.clear); // Runs this line of puzzle logic.
                    cell.SetPlayable(false); // Runs this line of puzzle logic.
                } // Ends this code block.
                // Column labels (A..)
                else if (y == 0) // Runs when the previous condition was false.
                { // Starts this code block.
                    char letter = (char)('A' + x - 1); // Runs this line of puzzle logic.
                    cell.SetLetter(letter); // Runs this line of puzzle logic.
                    cell.SetTextColor(rowColor); // Runs this line of puzzle logic.
                    cell.DisableCellUI(); // Runs this line of puzzle logic.
                    cell.SetPlayable(false); // Runs this line of puzzle logic.
                } // Ends this code block.
                // Row labels (1..)
                else if (x == 0) // Runs when the previous condition was false.
                { // Starts this code block.
                    cell.SetLetterString(y.ToString()); // Runs this line of puzzle logic.
                    cell.SetTextColor(ColumnColor); // Runs this line of puzzle logic.
                    cell.DisableCellUI(); // Runs this line of puzzle logic.
                    cell.SetPlayable(false); // Runs this line of puzzle logic.
                } // Ends this code block.
                else // Runs when the previous condition was false.
                { // Starts this code block.
                    cell.SetLetter(' '); // Runs this line of puzzle logic.
                    cell.SetTextColor(Color.black); // Runs this line of puzzle logic.
                    cell.SetPlayable(true); // Runs this line of puzzle logic.
                } // Ends this code block.

                grid[x, y] = cell; // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void PlaceFixedLettersFromSolution() // Declares a method used by this script.
    { // Starts this code block.
        int sizeY = Mathf.Min(gridSize, solution.GetLength(0)); // Creates a local value used by the following logic.
        int sizeX = Mathf.Min(gridSize, solution.GetLength(1)); // Creates a local value used by the following logic.

        for (int y = 1; y < sizeY; y++)       // skip header row
        { // Starts this code block.
            for (int x = 1; x < sizeX; x++)   // skip header col
            { // Starts this code block.
                char c = solution[y, x]; // Runs this line of puzzle logic.
                if (c != '\0') // Checks this condition before continuing.
                { // Starts this code block.
                    grid[x, y].SetLetter(c); // Runs this line of puzzle logic.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void FillRandomLetters() // Declares a method used by this script.
    { // Starts this code block.
        System.Random rand = new System.Random(BuildBoardSeed()); // Runs this line of puzzle logic.
        for (int y = 1; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 1; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                if (grid[x, y].GetLetter() == ' ') // Checks this condition before continuing.
                    grid[x, y].SetLetter((char)('A' + rand.Next(26))); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private int BuildBoardSeed() // Declares a method used by this script.
    { // Starts this code block.
        int seed = 17; // Creates a local value used by the following logic.
        string seedSource = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null // Creates a local value used by the following logic.
            ? PhotonNetwork.CurrentRoom.Name // Runs this line of puzzle logic.
            : gameObject.scene.name; // Runs this line of puzzle logic.

        foreach (char c in seedSource) // Repeats this block for each item in a collection.
            seed = (seed * 31) + c; // Updates this value.

        seed = (seed * 31) + gridSize; // Updates this value.
        return seed; // Returns this result to the caller.
    } // Ends this code block.

    private void SyncCellState(LetterCell cell) // Declares a method used by this script.
    { // Starts this code block.
        if (cell == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Vector2Int pos = cell.gridPosition; // Runs this line of puzzle logic.
        int state = EncodeCellState(cell.IsSelected, cell.CurrentResultState); // Creates a local value used by the following logic.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildCellStateKey(pos.x, pos.y)] = state // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        object[] eventData = // Runs this line of puzzle logic.
        { // Starts this code block.
            pos.x, // Runs this line of puzzle logic.
            pos.y, // Runs this line of puzzle logic.
            state // Runs this line of puzzle logic.
        }; // Runs this line of puzzle logic.

        PhotonNetwork.RaiseEvent( // Runs this line of puzzle logic.
            WordSearchCellStateEventCode, // Runs this line of puzzle logic.
            eventData, // Runs this line of puzzle logic.
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, // Runs this line of puzzle logic.
            SendOptions.SendReliable); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyRoomSyncedCellStates() // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        suppressNetworkStateApply = true; // Updates this value.

        for (int y = 1; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 1; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                string key = BuildCellStateKey(x, y); // Creates a local value used by the following logic.
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key)) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                ApplyEncodedStateToCell(x, y, (int)PhotonNetwork.CurrentRoom.CustomProperties[key]); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        suppressNetworkStateApply = false; // Updates this value.
    } // Ends this code block.

    private void ApplyEncodedStateToCell(int x, int y, int encodedState) // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || x <= 0 || y <= 0 || x >= gridSize || y >= gridSize) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        LetterCell cell = grid[x, y]; // Runs this line of puzzle logic.
        if (cell == null || !cell.IsPlayable) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        DecodeCellState(encodedState, out bool shouldSelect, out LetterCell.ResultVisualState resultState); // Runs this line of puzzle logic.
        ApplyCellState(cell, shouldSelect, resultState, false); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyCellState(LetterCell cell, bool shouldSelect, LetterCell.ResultVisualState resultState, bool sync) // Declares a method used by this script.
    { // Starts this code block.
        if (cell == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (shouldSelect) selected.Add(cell); // Checks this condition before continuing.
        else selected.Remove(cell); // Runs when the previous condition was false.

        cell.ApplySharedState(shouldSelect, resultState); // Runs this line of puzzle logic.

        if (sync && !suppressNetworkStateApply) // Checks this condition before continuing.
            SyncCellState(cell); // Runs this line of puzzle logic.
    } // Ends this code block.

    private static int EncodeCellState(bool isSelected, LetterCell.ResultVisualState resultState) // Declares a method used by this script.
    { // Starts this code block.
        return ((int)resultState << 1) | (isSelected ? 1 : 0); // Returns this result to the caller.
    } // Ends this code block.

    private static void DecodeCellState(int encodedState, out bool isSelected, out LetterCell.ResultVisualState resultState) // Declares a method used by this script.
    { // Starts this code block.
        isSelected = (encodedState & 1) == 1; // Updates this value.
        resultState = (LetterCell.ResultVisualState)(encodedState >> 1); // Updates this value.
    } // Ends this code block.

    private static string BuildCellStateKey(int x, int y) // Declares a method used by this script.
    { // Starts this code block.
        return $"{WordSearchCellStatePrefix}{x}_{y}"; // Returns this result to the caller.
    } // Ends this code block.

    private void ClearSyncedCellStates() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable clearedStates = new Hashtable(); // Creates a local value used by the following logic.

        for (int y = 1; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 1; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                clearedStates[BuildCellStateKey(x, y)] = 0; // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        PhotonNetwork.CurrentRoom.SetCustomProperties(clearedStates); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnEvent(EventData photonEvent) // Declares a method used by this script.
    { // Starts this code block.
        if (photonEvent.Code != WordSearchCellStateEventCode) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        if (!(photonEvent.CustomData is object[] data) || data.Length < 3) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        suppressNetworkStateApply = true; // Updates this value.
        ApplyEncodedStateToCell((int)data[0], (int)data[1], (int)data[2]); // Runs this line of puzzle logic.
        suppressNetworkStateApply = false; // Updates this value.
    } // Ends this code block.

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) // Overrides a Unity or Photon callback method.
    { // Starts this code block.
        if (grid == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        suppressNetworkStateApply = true; // Updates this value.

        foreach (DictionaryEntry entry in propertiesThatChanged) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.Key is not string key || !key.StartsWith(WordSearchCellStatePrefix)) // Checks this condition before continuing.
                continue; // Skips to the next loop iteration.

            string[] parts = key.Substring(WordSearchCellStatePrefix.Length).Split('_'); // Runs this line of puzzle logic.
            if (parts.Length != 2 || // Checks this condition before continuing.
                !int.TryParse(parts[0], out int x) || // Runs this line of puzzle logic.
                !int.TryParse(parts[1], out int y)) // Runs this line of puzzle logic.
                continue; // Skips to the next loop iteration.

            ApplyEncodedStateToCell(x, y, (int)entry.Value); // Runs this line of puzzle logic.
        } // Ends this code block.

        suppressNetworkStateApply = false; // Updates this value.
    } // Ends this code block.

    // ---------- Results clearing ----------
    private void ClearAllResults(bool sync = true) // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null) return; // Checks this condition before continuing.
        int sizeY = grid.GetLength(1); // Creates a local value used by the following logic.
        int sizeX = grid.GetLength(0); // Creates a local value used by the following logic.

        for (int y = 1; y < sizeY; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 1; x < sizeX; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                var cell = grid[x, y]; // Creates a local value used by the following logic.
                if (cell != null && cell.IsPlayable) // Checks this condition before continuing.
                    ApplyCellState(cell, selected.Contains(cell), LetterCell.ResultVisualState.None, sync); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    // =========================
    //  Selection API for cells
    // =========================
    public void BeginDrag(LetterCell start) // Declares a method used by this script.
    { // Starts this code block.
        if (!start.IsPlayable) return; // Checks this condition before continuing.

        isDragging = true; // Updates this value.
        dragAddMode = !selected.Contains(start); // start on selected => unselect mode

        if (dragAddMode) SelectCell(start); // Checks this condition before continuing.
        else UnselectCell(start); // Runs when the previous condition was false.
    } // Ends this code block.

    public void DragOver(LetterCell cell) // Declares a method used by this script.
    { // Starts this code block.
        if (!isDragging || !cell.IsPlayable) return; // Checks this condition before continuing.

        if (dragAddMode) SelectCell(cell); // Checks this condition before continuing.
        else UnselectCell(cell); // Runs when the previous condition was false.
    } // Ends this code block.

    public void EndDrag() // Declares a method used by this script.
    { // Starts this code block.
        isDragging = false; // Updates this value.
    } // Ends this code block.

    private void SelectCell(LetterCell cell) // Declares a method used by this script.
    { // Starts this code block.
        if (!selected.Contains(cell) || cell.CurrentResultState != LetterCell.ResultVisualState.None) // Checks this condition before continuing.
            ApplyCellState(cell, true, LetterCell.ResultVisualState.None, true); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void UnselectCell(LetterCell cell) // Declares a method used by this script.
    { // Starts this code block.
        if (selected.Contains(cell) || cell.CurrentResultState != LetterCell.ResultVisualState.None) // Checks this condition before continuing.
            ApplyCellState(cell, false, LetterCell.ResultVisualState.None, true); // Runs this line of puzzle logic.
    } // Ends this code block.

    // Optional public single-click toggle (BeginDrag already handles it)
    public void ToggleCell(LetterCell cell) // Declares a method used by this script.
    { // Starts this code block.
        if (!cell.IsPlayable) return; // Checks this condition before continuing.
        if (selected.Contains(cell)) UnselectCell(cell); // Checks this condition before continuing.
        else SelectCell(cell); // Runs when the previous condition was false.
    } // Ends this code block.

    // Keep button wiring simple
    public void Submit() => IsSolved(); // Declares a method used by this script.

    // =========================
    // Final check + painter
    // =========================
    // True iff:
    //   1) every solution cell is selected AND matches its letter, and
    //   2) no extra cells are selected outside the solution map.
    // Also: clears previous results, then paints whole-word results green/red.
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        ClearAllResults(); // fresh attempt paint

        bool allMatch = true; // Creates a local value used by the following logic.
        var expectedCells = new HashSet<LetterCell>(); // All cells belonging to a target word.
        var correctCells = new HashSet<LetterCell>(); // Cells in fully selected target words.
        var wrongCells = new HashSet<LetterCell>(); // Selected cells in incomplete target words.

        // 1) Evaluate each word as a complete selection. A correct letter is only
        // correct when every letter in that word has also been selected.
        foreach (string expectedWord in ExpectedWordReferences)
        { // Starts this code block.
            if (!TryFindWordPositions(expectedWord, out List<Vector2Int> positions))
            {
                allMatch = false;
                continue; // Skips to the next loop iteration.
            }

            bool wordIsComplete = true;
            var wordCells = new List<LetterCell>();
            for (int index = 0; index < positions.Count; index++)
            {
                Vector2Int position = positions[index];
                LetterCell cell = grid[position.x, position.y];
                if (cell != null)
                {
                    expectedCells.Add(cell);
                    wordCells.Add(cell);
                }

                if (cell == null || !selected.Contains(cell) ||
                    char.ToUpperInvariant(cell.GetLetter()) != char.ToUpperInvariant(expectedWord[index]))
                {
                    wordIsComplete = false;
                }
            }

            if (wordIsComplete)
            {
                foreach (LetterCell cell in wordCells)
                    correctCells.Add(cell);
            }
            else
            {
                allMatch = false;
                foreach (LetterCell cell in wordCells)
                {
                    if (selected.Contains(cell))
                        wrongCells.Add(cell);
                }
            }
        } // Ends this code block.

        // 2) Paint selected cells. Red takes precedence when a cell belongs to an
        // incomplete word, including cells shared with an otherwise complete word.
        foreach (LetterCell cell in selected)
        {
            LetterCell.ResultVisualState resultState;
            if (wrongCells.Contains(cell) || !expectedCells.Contains(cell))
            { // Starts this code block.
                resultState = LetterCell.ResultVisualState.Wrong;
                allMatch = false;
            }
            else
            {
                resultState = correctCells.Contains(cell)
                    ? LetterCell.ResultVisualState.Correct
                    : LetterCell.ResultVisualState.Wrong;
                if (resultState == LetterCell.ResultVisualState.Wrong)
                    allMatch = false;
            }

            ApplyCellState(cell, true, resultState, true);
        }

        return allMatch; // Returns this result to the caller.
    } // Ends this code block.

    private static readonly string[] ExpectedWordReferences =
    {
        "FLUOROQUINOLONES", "SULFONYLUREAS", "LINEZOLID", "AZOLES", "ELDERLY", "IMPAIRED", "REDUCTION"
    };

    private static readonly string[] ExpectedWordDisplayReferences =
    {
        "Fluoroquinolones", "Sulfonylureas", "Linezolid", "Azoles", "Elderly", "Impaired", "reduction"
    };

    public string GetCorrectAnswerSnapshot()
    {
        return string.Join(", ", ExpectedWordDisplayReferences);
    }

    public string GetWrongAnswerSnapshot() // Returns each incorrect word with its clue-reference answer.
    {
        var entries = new List<string>();
        var expectedCells = new HashSet<LetterCell>();
        int matchedWordCount = 0;

        foreach (string expectedWord in ExpectedWordReferences)
        {
            if (!TryFindWordPositions(expectedWord, out List<Vector2Int> positions))
                continue;

            matchedWordCount++;
            var actualWord = new System.Text.StringBuilder();
            var missingPositions = new List<int>();
            var extraLetters = new List<string>();
            for (int index = 0; index < positions.Count; index++)
            {
                Vector2Int position = positions[index];
                LetterCell cell = grid[position.x, position.y];
                if (cell != null)
                    expectedCells.Add(cell);

                if (cell == null || !selected.Contains(cell))
                {
                    actualWord.Append('_' );
                    missingPositions.Add(index + 1);
                    continue;
                }

                char actual = cell.GetLetter();
                actualWord.Append(actual);
                if (char.ToUpperInvariant(actual) != char.ToUpperInvariant(expectedWord[index]))
                    extraLetters.Add(actual.ToString());
            }

            if (missingPositions.Count > 0)
                entries.Add($"Missing letter(s) for {expectedWord}: position(s) {string.Join(", ", missingPositions)} [expected {expectedWord}]");
            if (extraLetters.Count > 0)
                entries.Add($"Extra letter(s) for {expectedWord}: {string.Join(", ", extraLetters)} [expected {expectedWord}]");
        }

        var unrelatedSelectedLetters = new List<string>();
        foreach (var cell in selected)
        {
            if (cell != null && !expectedCells.Contains(cell))
                unrelatedSelectedLetters.Add(cell.GetLetter().ToString());
        }
        if (unrelatedSelectedLetters.Count > 0)
            entries.Add(string.Join(", ", unrelatedSelectedLetters));

        if (entries.Count == 0 && matchedWordCount == 0)
            entries.Add($"Missing word(s): {string.Join(", ", ExpectedWordReferences)}");
        if (entries.Count == 0)
            entries.Add("No wrong word search answer");

        return string.Join(", ", entries);
    }

    private bool TryFindWordPositions(string word, out List<Vector2Int> positions)
    {
        positions = new List<Vector2Int>();
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };
        for (int y = 1; y < gridSize; y++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                foreach (Vector2Int direction in directions)
                {
                    var candidate = new List<Vector2Int>();
                    bool matches = true;
                    for (int i = 0; i < word.Length; i++)
                    {
                        int currentX = x + direction.x * i;
                        int currentY = y + direction.y * i;
                        if (currentX <= 0 || currentY <= 0 || currentX >= gridSize || currentY >= gridSize || char.ToUpperInvariant(solution[currentY, currentX]) != word[i])
                        {
                            matches = false;
                            break;
                        }
                        candidate.Add(new Vector2Int(currentX, currentY));
                    }

                    if (matches)
                    {
                        positions = candidate;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private string GetLegacyWrongAnswerSnapshot() // Retains the previous cell-level implementation.
    {
        var entries = new List<string>();
        int sizeY = Mathf.Min(gridSize, solution.GetLength(0));
        int sizeX = Mathf.Min(gridSize, solution.GetLength(1));
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1) };

        foreach (Vector2Int direction in directions)
        {
            for (int y = 1; y < sizeY; y++)
            {
                for (int x = 1; x < sizeX; x++)
                {
                    if (solution[y, x] == '\0') continue;
                    int previousX = x - direction.x;
                    int previousY = y - direction.y;
                    if (previousX > 0 && previousY > 0 && previousX < sizeX && previousY < sizeY && solution[previousY, previousX] != '\0') continue;

                    var positions = new List<Vector2Int>();
                    int currentX = x;
                    int currentY = y;
                    while (currentX > 0 && currentY > 0 && currentX < sizeX && currentY < sizeY && solution[currentY, currentX] != '\0')
                    {
                        positions.Add(new Vector2Int(currentX, currentY));
                        currentX += direction.x;
                        currentY += direction.y;
                    }

                    if (positions.Count < 2) continue;
                    var actualWord = new System.Text.StringBuilder();
                    var expectedWord = new System.Text.StringBuilder();
                    bool incorrect = false;
                    foreach (Vector2Int position in positions)
                    {
                        LetterCell cell = grid[position.x, position.y];
                        char expected = solution[position.y, position.x];
                        expectedWord.Append(expected);
                        if (cell == null || !selected.Contains(cell))
                        {
                            actualWord.Append('_');
                            incorrect = true;
                        }
                        else
                        {
                            char actual = cell.GetLetter();
                            actualWord.Append(actual);
                            if (char.ToUpperInvariant(actual) != char.ToUpperInvariant(expected)) incorrect = true;
                        }
                    }

                    if (incorrect)
                        entries.Add($"Wrong word={actualWord} [expected {expectedWord}]");
                }
            }
        }

        if (entries.Count == 0)
        {
            foreach (var cell in selected)
            {
                if (cell == null) continue;
                Vector2Int position = cell.gridPosition;
                if (position.x <= 0 || position.y <= 0 || position.x >= sizeX || position.y >= sizeY) continue;
                if (solution[position.y, position.x] == '\0')
                    entries.Add($"Wrong word={cell.GetLetter()} [expected no solution]");
            }
        }

        return entries.Count > 0 ? string.Join(", ", entries) : "No wrong word search answer";
    }

    private string GetCellWrongAnswerSnapshot() // Retains the cell-level fallback implementation.
    {
        var entries = new List<string>();
        var seen = new HashSet<string>();
        int sizeY = Mathf.Min(gridSize, solution.GetLength(0));
        int sizeX = Mathf.Min(gridSize, solution.GetLength(1));

        var selectedPositions = new List<Vector2Int>();
        foreach (var selectedCell in selected)
        {
            if (selectedCell == null) continue;
            Vector2Int position = selectedCell.gridPosition;
            if (position.x > 0 && position.y > 0 && position.x < sizeX && position.y < sizeY)
                selectedPositions.Add(position);
        }

        if (selectedPositions.Count > 0)
        {
            selectedPositions.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var actualWord = new System.Text.StringBuilder();
            var expectedWord = new System.Text.StringBuilder();
            foreach (Vector2Int position in selectedPositions)
            {
                actualWord.Append(grid[position.x, position.y].GetLetter());
                char expected = solution[position.y, position.x];
                expectedWord.Append(expected == '\0' ? '?' : expected);
            }

            return $"Wrong word={actualWord} [expected {expectedWord}]";
        }

        foreach (var cell in selected)
        {
            if (cell == null) continue;
            Vector2Int position = cell.gridPosition;
            if (position.x <= 0 || position.y <= 0 || position.x >= sizeX || position.y >= sizeY) continue;

            char expected = solution[position.y, position.x];
            char actual = cell.GetLetter();
            if (expected == '\0')
            {
                string extraKey = $"{position.x},{position.y}";
                if (seen.Add(extraKey))
                    entries.Add($"Cell({position.x},{position.y})={actual} [expected no solution]");
                continue;
            }
            if (char.ToUpperInvariant(actual) == char.ToUpperInvariant(expected)) continue;

            string key = $"{position.x},{position.y}";
            if (seen.Add(key))
                entries.Add($"Cell({position.x},{position.y})={actual} [expected {expected}]");
        }

        for (int y = 1; y < sizeY; y++)
        {
            for (int x = 1; x < sizeX; x++)
            {
                char expected = solution[y, x];
                if (expected == '\0') continue;

                LetterCell cell = grid[x, y];
                if (cell != null && selected.Contains(cell)) continue;
                string key = $"{x},{y}";
                if (seen.Add(key))
                    entries.Add($"Cell({x},{y})=not selected [expected {expected}]");
            }
        }

        return entries.Count > 0 ? string.Join("; ", entries) : "No wrong word search answer";
    }

    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        // --- 1) Destroy all existing grid children ---
        if (gridPanel == null) // Checks this condition before continuing.
        { // Starts this code block.
            Debug.LogError("[WordSearch] gridPanel is NULL"); // Writes an error message to the Unity console.
            return; // Returns this result to the caller.
        } // Ends this code block.

        Debug.Log($"[WordSearch] ResetPuzzle - children BEFORE = {gridPanel.transform.childCount}"); // Writes a diagnostic message to the Unity console.

        for (int i = gridPanel.transform.childCount - 1; i >= 0; i--) // Repeats this block for a counted loop.
        { // Starts this code block.
            Transform child = gridPanel.transform.GetChild(i); // Creates a local value used by the following logic.
            Destroy(child.gameObject); // Runs this line of puzzle logic.
        } // Ends this code block.

        // --- 2) Reset selection state ---
        selected.Clear(); // Runs this line of puzzle logic.
        isDragging = false; // Updates this value.
        suppressNetworkStateApply = false; // Updates this value.
        ClearSyncedCellStates(); // Runs this line of puzzle logic.

        grid = null; // Updates this value.

        // --- 3) Regenerate everything ---
        GenerateGrid(); // Runs this line of puzzle logic.
        PlaceFixedLettersFromSolution(); // Runs this line of puzzle logic.
        FillRandomLetters(); // Runs this line of puzzle logic.
        ApplyRoomSyncedCellStates(); // Runs this line of puzzle logic.

        Debug.Log($"[WordSearch] ResetPuzzle - DONE, children AFTER = {gridPanel.transform.childCount}"); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.

} // Ends this code block.
