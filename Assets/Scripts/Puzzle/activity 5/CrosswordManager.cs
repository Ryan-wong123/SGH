using System; // Imports a namespace used by this script.
using System.Collections.Generic; // Imports a namespace used by this script.
using System.Text; // Imports a namespace used by this script.
using ExitGames.Client.Photon; // Imports a namespace used by this script.
using Photon.Pun; // Imports a namespace used by this script.
using Photon.Realtime; // Imports a namespace used by this script.
using UnityEngine; // Imports a namespace used by this script.
using TMPro; // Imports a namespace used by this script.
using UnityEngine.Events; // Imports a namespace used by this script.
using Hashtable = ExitGames.Client.Photon.Hashtable; // Imports a namespace used by this script.

public class CrosswordManager : MonoBehaviour, IPuzzle, IOnEventCallback // Declares this type.
{ // Starts this code block.
    private const byte CrosswordTextSyncEventCode = 41; // Declares a field, property, or member used by this script.
    private const byte CrosswordResultSyncEventCode = 43; // Declares a field, property, or member used by this script.
    private const string CrosswordCellPropertyPrefix = "P5CW_"; // Declares a field, property, or member used by this script.
    private const string CrosswordCellResultPropertyPrefix = "P9CW_RESULT_"; // Declares a field, property, or member used by this script.

    private enum CrosswordCellResultState // Declares this type.
    { // Starts this code block.
        None = 0, // Updates this value.
        Correct = 1, // Updates this value.
        Wrong = 2 // Updates this value.
    } // Ends this code block.

    public GameObject cellPrefab; // Declares a field, property, or member used by this script.
    public Transform gridParent; // A GridLayoutGroup
    public int gridSize = 13; // Declares a field, property, or member used by this script.

    private CrosswordCell[,] grid; // Declares a field, property, or member used by this script.
    private readonly Dictionary<TMP_InputField, UnityAction<string>> inputListeners = new Dictionary<TMP_InputField, UnityAction<string>>(); // Declares a field, property, or member used by this script.
    private readonly Dictionary<int, int> lastSeqBySender = new Dictionary<int, int>(); // Declares a field, property, or member used by this script.
    private bool suppressInputSync; // Declares a field, property, or member used by this script.
    private int localSeq; // Declares a field, property, or member used by this script.

    private char[,] solution = new char[13, 13] // Declares a field, property, or member used by this script.
    { // Starts this code block.
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','l','\0','\0','\0' },   // Row 0
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','a','\0','\0','\0' },   // Row 1
        { '\0','\0','l','i','s','p','r','o','\0','n','p','h','\0' },           // Row 2
        { '\0','\0','\0','\0','\0','\0','e','\0','\0','t','\0','\0','\0' },    // Row 3
        { '\0','\0','\0','\0','\0','\0','g','\0','\0','u','\0','\0','\0' },    // Row 4
        { '\0','\0','\0','\0','g','l','u','l','i','s','i','n','e' },           // Row 5
        { '\0','\0','\0','\0','\0','\0','l','\0','\0','\0','\0','\0','\0'},    // Row 6      
        { '\0','\0','\0','\0','\0','\0','a','s','p','a','r','t','\0' },        // Row 7
        { 'd','e','t','e','m','i','r','\0','\0','\0','\0','o','\0' },          // Row 8
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','u','\0' },   // Row 9
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','j','\0' },   // Row 10
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','e','\0' },   // Row 11
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','o','\0' },   // Row 12                                                 
    }; // Runs this line of puzzle logic.



    void Start() // Declares a method used by this script.
    { // Starts this code block.
        GenerateGrid(); // Runs this line of puzzle logic.
        ApplyRoomSyncedTexts(); // Runs this line of puzzle logic.
        ApplyRoomSyncedResultStates(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnEnable() // Declares a method used by this script.
    { // Starts this code block.
        PhotonNetwork.AddCallbackTarget(this); // Runs this line of puzzle logic.
        RegisterExistingGridListeners(); // Runs this line of puzzle logic.
        ApplyRoomSyncedTexts(); // Runs this line of puzzle logic.
        ApplyRoomSyncedResultStates(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnDisable() // Declares a method used by this script.
    { // Starts this code block.
        PhotonNetwork.RemoveCallbackTarget(this); // Runs this line of puzzle logic.
        UnregisterGridListeners(); // Runs this line of puzzle logic.
    } // Ends this code block.

    void GenerateGrid() // Declares a method used by this script.
    { // Starts this code block.
        grid = new CrosswordCell[gridSize, gridSize]; // Updates this value.
        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                GameObject cellGO = Instantiate(cellPrefab, gridParent); // Creates a local value used by the following logic.
                CrosswordCell cell = cellGO.GetComponent<CrosswordCell>(); // Runs this line of puzzle logic.

                int visualY = gridSize - 1 - y; // flip vertically to match Unity's layout
                cell.position = new Vector2Int(x, visualY); // Updates this value.

                char letter = solution[y, x]; // Runs this line of puzzle logic.
                bool active = letter != '\0'; // Creates a local value used by the following logic.
                cell.SetInteractable(active); // Runs this line of puzzle logic.
                grid[y, x] = cell; // Runs this line of puzzle logic.

                if (active && cell.inputField != null) // Checks this condition before continuing.
                    RegisterCellListener(cell, x, y); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void RegisterCellListener(CrosswordCell cell, int x, int y) // Declares a method used by this script.
    { // Starts this code block.
        TMP_InputField inputField = cell.inputField; // Runs this line of puzzle logic.
        if (inputField == null || inputListeners.ContainsKey(inputField)) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        UnityAction<string> listener = value => OnCellValueChanged(x, y, value); // Runs this line of puzzle logic.
        inputField.onValueChanged.AddListener(listener); // Runs this line of puzzle logic.
        inputListeners[inputField] = listener; // Updates this value.
    } // Ends this code block.

    private void RegisterExistingGridListeners() // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                CrosswordCell cell = grid[y, x]; // Runs this line of puzzle logic.
                if (cell == null || cell.inputField == null || !cell.isActiveAndEnabled || !cell.inputField.interactable) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                RegisterCellListener(cell, x, y); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void UnregisterGridListeners() // Declares a method used by this script.
    { // Starts this code block.
        foreach (var entry in inputListeners) // Repeats this block for each item in a collection.
        { // Starts this code block.
            if (entry.Key != null) // Checks this condition before continuing.
                entry.Key.onValueChanged.RemoveListener(entry.Value); // Runs this line of puzzle logic.
        } // Ends this code block.

        inputListeners.Clear(); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void OnCellValueChanged(int x, int y, string value) // Declares a method used by this script.
    { // Starts this code block.
        if (suppressInputSync || !PhotonNetwork.InRoom) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        localSeq++; // Runs this line of puzzle logic.
        SyncCellTextProperty(x, y, value); // Runs this line of puzzle logic.

        object[] content = // Runs this line of puzzle logic.
        { // Starts this code block.
            x, // Runs this line of puzzle logic.
            y, // Runs this line of puzzle logic.
            value ?? string.Empty, // Runs this line of puzzle logic.
            PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1, // Runs this line of puzzle logic.
            localSeq // Runs this line of puzzle logic.
        }; // Runs this line of puzzle logic.

        PhotonNetwork.RaiseEvent( // Runs this line of puzzle logic.
            CrosswordTextSyncEventCode, // Runs this line of puzzle logic.
            content, // Runs this line of puzzle logic.
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, // Runs this line of puzzle logic.
            SendOptions.SendReliable); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void SyncCellTextProperty(int x, int y, string value) // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildCellPropertyKey(x, y)] = value ?? string.Empty // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.
    } // Ends this code block.

    public void OnEvent(EventData photonEvent) // Declares a method used by this script.
    { // Starts this code block.
        if (photonEvent.Code != CrosswordTextSyncEventCode) // Checks this condition before continuing.
        { // Starts this code block.
            if (photonEvent.Code != CrosswordResultSyncEventCode) // Checks this condition before continuing.
                return; // Returns this result to the caller.

            if (!(photonEvent.CustomData is object[] resultData) || resultData.Length < 3) // Checks this condition before continuing.
                return; // Returns this result to the caller.

            ApplyRemoteResultState((int)resultData[0], (int)resultData[1], (CrosswordCellResultState)(int)resultData[2]); // Runs this line of puzzle logic.
            return; // Returns this result to the caller.
        } // Ends this code block.

        if (!(photonEvent.CustomData is object[] data) || data.Length < 5) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        int x = (int)data[0]; // Creates a local value used by the following logic.
        int y = (int)data[1]; // Creates a local value used by the following logic.
        string text = (string)data[2]; // Creates a local value used by the following logic.
        int actor = (int)data[3]; // Creates a local value used by the following logic.
        int seq = (int)data[4]; // Creates a local value used by the following logic.

        int senderKey = (actor * 1000) + (y * gridSize) + x; // Creates a local value used by the following logic.
        if (lastSeqBySender.TryGetValue(senderKey, out int lastSeq) && seq <= lastSeq) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        lastSeqBySender[senderKey] = seq; // Updates this value.
        ApplyRemoteText(x, y, text); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyRemoteText(int x, int y, string text) // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || y < 0 || y >= gridSize || x < 0 || x >= gridSize) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        CrosswordCell cell = grid[y, x]; // Runs this line of puzzle logic.
        if (cell == null || cell.inputField == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        suppressInputSync = true; // Updates this value.
        cell.inputField.text = text; // Updates this value.
        suppressInputSync = false; // Updates this value.
    } // Ends this code block.

    private void ApplyRoomSyncedTexts() // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                CrosswordCell cell = grid[y, x]; // Runs this line of puzzle logic.
                if (cell == null || cell.inputField == null || !cell.inputField.interactable) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                string key = BuildCellPropertyKey(x, y); // Creates a local value used by the following logic.
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key)) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                ApplyRemoteText(x, y, PhotonNetwork.CurrentRoom.CustomProperties[key] as string ?? string.Empty); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void ApplyRoomSyncedResultStates() // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                CrosswordCell cell = grid[y, x]; // Runs this line of puzzle logic.
                if (cell == null || cell.inputField == null || !cell.inputField.interactable) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                string key = BuildCellResultPropertyKey(x, y); // Creates a local value used by the following logic.
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key)) // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                ApplyRemoteResultState(x, y, (CrosswordCellResultState)(int)PhotonNetwork.CurrentRoom.CustomProperties[key]); // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.
    } // Ends this code block.

    private void ClearSyncedCellTexts() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable clearedValues = new Hashtable(); // Creates a local value used by the following logic.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                if (solution[y, x] == '\0') // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                clearedValues[BuildCellPropertyKey(x, y)] = string.Empty; // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        PhotonNetwork.CurrentRoom.SetCustomProperties(clearedValues); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ClearSyncedCellResults() // Declares a method used by this script.
    { // Starts this code block.
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Hashtable clearedValues = new Hashtable(); // Creates a local value used by the following logic.

        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                if (solution[y, x] == '\0') // Checks this condition before continuing.
                    continue; // Skips to the next loop iteration.

                clearedValues[BuildCellResultPropertyKey(x, y)] = (int)CrosswordCellResultState.None; // Runs this line of puzzle logic.
            } // Ends this code block.
        } // Ends this code block.

        PhotonNetwork.CurrentRoom.SetCustomProperties(clearedValues); // Runs this line of puzzle logic.
    } // Ends this code block.

    private static string BuildCellPropertyKey(int x, int y) // Declares a method used by this script.
    { // Starts this code block.
        return $"{CrosswordCellPropertyPrefix}{x}_{y}"; // Returns this result to the caller.
    } // Ends this code block.

    private static string BuildCellResultPropertyKey(int x, int y) // Declares a method used by this script.
    { // Starts this code block.
        return $"{CrosswordCellResultPropertyPrefix}{x}_{y}"; // Returns this result to the caller.
    } // Ends this code block.

    private void SetCellResultState(int x, int y, CrosswordCellResultState state, bool sync = true) // Declares a method used by this script.
    { // Starts this code block.
        if (grid == null || y < 0 || y >= gridSize || x < 0 || x >= gridSize) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        CrosswordCell cell = grid[y, x]; // Runs this line of puzzle logic.
        if (cell == null || cell.inputField == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        ApplyInputFieldResultColor(cell.inputField, state); // Runs this line of puzzle logic.

        if (!sync || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable // Runs this line of puzzle logic.
        { // Starts this code block.
            [BuildCellResultPropertyKey(x, y)] = (int)state // Adds an attribute that changes how Unity or C# treats the next declaration.
        }); // Runs this line of puzzle logic.

        PhotonNetwork.RaiseEvent( // Runs this line of puzzle logic.
            CrosswordResultSyncEventCode, // Runs this line of puzzle logic.
            new object[] { x, y, (int)state }, // Runs this line of puzzle logic.
            new RaiseEventOptions { Receivers = ReceiverGroup.Others }, // Runs this line of puzzle logic.
            SendOptions.SendReliable); // Runs this line of puzzle logic.
    } // Ends this code block.

    private void ApplyRemoteResultState(int x, int y, CrosswordCellResultState state) // Declares a method used by this script.
    { // Starts this code block.
        SetCellResultState(x, y, state, false); // Runs this line of puzzle logic.
    } // Ends this code block.

    private static void ApplyInputFieldResultColor(TMP_InputField inputField, CrosswordCellResultState state) // Declares a method used by this script.
    { // Starts this code block.
        if (inputField == null) // Checks this condition before continuing.
            return; // Returns this result to the caller.

        Color color = Color.white; // Creates a local value used by the following logic.
        if (state == CrosswordCellResultState.Correct) // Checks this condition before continuing.
            color = Color.green; // Creates a local value used by the following logic.
        else if (state == CrosswordCellResultState.Wrong) // Runs when the previous condition was false.
            color = Color.red; // Creates a local value used by the following logic.

        if (inputField.image != null) // Checks this condition before continuing.
            inputField.image.color = color; // Updates this value.
    } // Ends this code block.



    public string GetWrongAnswerSnapshot()
    {
        var entries = new List<string>();
        var seen = new HashSet<string>();

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (solution[y, x] == '\0' || char.ToUpper(grid[y, x].GetLetter()) == char.ToUpper(solution[y, x]))
                    continue;

                AddWrongWord(entries, seen, x, y, 1, 0, "Across");
                AddWrongWord(entries, seen, x, y, 0, 1, "Down");
            }
        }

        return entries.Count > 0 ? string.Join("; ", entries) : "No wrong crossword word";
    }

    private void AddWrongWord(List<string> entries, HashSet<string> seen, int x, int y, int dx, int dy, string direction)
    {
        int startX = x;
        int startY = y;
        while (IsActiveCell(startX - dx, startY - dy))
        {
            startX -= dx;
            startY -= dy;
        }

        string key = $"{direction}:{startX}:{startY}";
        if (!seen.Add(key))
            return;

        var typed = new StringBuilder();
        var expected = new StringBuilder();
        bool hasWrong = false;
        int wordLength = 0;
        for (int cx = startX, cy = startY; IsActiveCell(cx, cy); cx += dx, cy += dy)
        {
            char actual = grid[cy, cx].GetLetter();
            char answer = solution[cy, cx];
            typed.Append(actual == ' ' ? '_' : char.ToUpper(actual));
            expected.Append(char.ToUpper(answer));
            hasWrong |= char.ToUpper(actual) != char.ToUpper(answer);
            wordLength++;
        }

        if (wordLength > 1 && hasWrong)
        {
            int clueNumber = GetClueNumber(startX, startY, direction);
            string clueLabel = clueNumber > 0 ? $"{direction} clue {clueNumber}" : $"{direction} clue ({startX},{startY})";
            entries.Add($"{clueLabel}={typed} [expected {expected}]");
        }
    }

    private static int GetClueNumber(int startX, int startY, string direction)
    {
        if (direction == "Across")
        {
            if (startX == 2 && startY == 2) return 2;
            if (startX == 4 && startY == 5) return 4;
            if (startX == 6 && startY == 7) return 5;
            if (startX == 0 && startY == 8) return 6;
            if (startX == 9 && startY == 2) return 8;
        }
        else if (direction == "Down")
        {
            if (startX == 9 && startY == 0) return 1;
            if (startX == 6 && startY == 2) return 3;
            if (startX == 11 && startY == 7) return 7;
        }

        return 0;
    }

    private bool IsActiveCell(int x, int y)
    {
        return x >= 0 && x < gridSize && y >= 0 && y < gridSize && solution[y, x] != '\0';
    }

    public bool CheckPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        bool correct = true; // Creates a local value used by the following logic.
        for (int y = 0; y < gridSize; y++) // Repeats this block for a counted loop.
        { // Starts this code block.
            for (int x = 0; x < gridSize; x++) // Repeats this block for a counted loop.
            { // Starts this code block.
                if (solution[y, x] == '\0') continue; // Checks this condition before continuing.

                char expected = solution[y, x]; // Runs this line of puzzle logic.
                char typed = grid[y, x].GetLetter(); // Runs this line of puzzle logic.

                if (char.ToUpper(typed) != char.ToUpper(expected)) // Checks this condition before continuing.
                { // Starts this code block.
                    correct = false; // Updates this value.
                    SetCellResultState(x, y, CrosswordCellResultState.Wrong); // Runs this line of puzzle logic.
                } // Ends this code block.
                else // Runs when the previous condition was false.
                { // Starts this code block.
                    SetCellResultState(x, y, CrosswordCellResultState.Correct); // Runs this line of puzzle logic.
                } // Ends this code block.
            } // Ends this code block.
        } // Ends this code block.

        return correct; // return result
    } // Ends this code block.
    public bool IsSolved() // Declares a method used by this script.
    { // Starts this code block.
        return CheckPuzzle(); // Returns this result to the caller.
    } // Ends this code block.
    public void ResetPuzzle() // Declares a method used by this script.
    { // Starts this code block.
        UnregisterGridListeners(); // Runs this line of puzzle logic.
        lastSeqBySender.Clear(); // Runs this line of puzzle logic.
        localSeq = 0; // Updates this value.
        ClearSyncedCellTexts(); // Runs this line of puzzle logic.
        ClearSyncedCellResults(); // Runs this line of puzzle logic.

        // Destroy old grid first
        foreach (Transform child in gridParent) // Repeats this block for each item in a collection.
        { // Starts this code block.
            Destroy(child.gameObject); // Runs this line of puzzle logic.
        } // Ends this code block.

        // Recreate the grid fresh
        GenerateGrid(); // Runs this line of puzzle logic.
        ApplyRoomSyncedTexts(); // Runs this line of puzzle logic.
        ApplyRoomSyncedResultStates(); // Runs this line of puzzle logic.

        Debug.Log("Crossword puzzle reset."); // Writes a diagnostic message to the Unity console.
    } // Ends this code block.



} // Ends this code block.
