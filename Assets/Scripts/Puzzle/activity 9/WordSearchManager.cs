using System.Collections.Generic;
using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using TMPro;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class WordSearchManager : MonoBehaviourPunCallbacks, IPuzzle, IOnEventCallback
{
    private const byte WordSearchCellStateEventCode = 42;
    private const string WordSearchCellStatePrefix = "P5WS_";

    [Header("Grid Setup")]
    public GameObject gridPanel;
    public GameObject cellPrefab;
    [Range(2, 40)] public int gridSize = 20; // includes header row/col

    [Header("Letters")]
    [SerializeField] private bool useFixedPlacement = false; // kept for compatibility; placement is always applied below

    // Selection state
    private bool isDragging = false;
    private bool dragAddMode = true; // true: selecting, false: unselecting
    private readonly HashSet<LetterCell> selected = new();
    private bool suppressNetworkStateApply;

    // Grid
    private LetterCell[,] grid;

    [SerializeField] private Color rowColor;
    [SerializeField] private Color ColumnColor;

    private bool _built = false;
    private static WordSearchManager _activeInstance;

    // Solution map: '\0' means "no specific correct letter for this cell"
    private char[,] solution = new char[21, 21]
    {
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L','I','N','E','Z','O','L','I','D' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S'  },
        { '\0','\0','\0','\0','\0','\0','R','E','D','U','C','T','I','O','N','F','\0','\0','\0','\0','U'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','L'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','U','\0','\0','\0','\0','F'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','O'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','N'  },
        { '\0','A','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','Y'  },
        { '\0','Z','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','Q','\0','\0','\0','\0','L'  },
        { '\0','O','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','U','\0','\0','\0','\0','U'  },
        { '\0','L','\0','\0','\0','\0','\0','E','\0','\0','\0','\0','\0','\0','\0','I','\0','\0','\0','\0','R'  },
        { '\0','E','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','\0','\0','\0','N','\0','\0','\0','\0','E'  },
        { '\0','S','\0','\0','\0','\0','\0','D','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','A'  },
        { '\0','I','M','P','A','I','R','E','D','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','S'  },
        { '\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','\0','\0','\0','N','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','Y','\0','\0','\0','\0','\0','\0','\0','E','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' }
    };

    private void Awake()
    {
        // HARD BLOCK: only 1 active WordSearchManager at runtime
        if (_activeInstance != null && _activeInstance != this)
        {
            Debug.LogWarning($"[WordSearch] Duplicate WordSearchManager detected -> DISABLING this one on {gameObject.name}");
            gameObject.SetActive(false);
            return;
        }
        _activeInstance = this;
    }

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);

        if (_built)
            ApplyRoomSyncedCellStates();
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);

        if (_activeInstance == this && !gameObject.activeInHierarchy)
            _activeInstance = null;
    }

    private void Start()
    {
        // If something calls Start twice / object re-enabled, don’t build again
        if (_built) return;

        // Always start clean (prevents any leftover children if prefab already contains them)
        ClearGridPanelImmediate();

        GenerateGrid();
        PlaceFixedLettersFromSolution();
        FillRandomLetters();
        ApplyRoomSyncedCellStates();

        _built = true;

        Debug.Log($"[WordSearch] BUILT on '{gridPanel.name}' path='{GetTransformPath(gridPanel.transform)}' " +
                  $"children={gridPanel.transform.childCount} expected={gridSize * gridSize}");

        // GLOBAL proof: if you see a 2nd grid, this number will be > expected overall
        int totalCells = FindObjectsOfType<LetterCell>(true).Length;
        Debug.Log($"[WordSearch] TOTAL LetterCell in scene (all parents) = {totalCells}");
    }
    private void ClearGridPanelImmediate()
    {
        if (gridPanel == null) return;

        // runtime-safe: Destroy existing children (if any)
        for (int i = gridPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(gridPanel.transform.GetChild(i).gameObject);
        }
    }
    private static string GetTransformPath(Transform t)
    {
        var stack = new Stack<string>();
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }
    private void GenerateGrid()
    {
        grid = new LetterCell[gridSize, gridSize];

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var obj = Instantiate(cellPrefab, gridPanel.transform);
                var cell = obj.GetComponent<LetterCell>();
                cell.Init(this, new Vector2Int(x, y));

                // Top-left corner
                if (x == 0 && y == 0)
                {
                    cell.SetLetter(' ');
                    cell.DisableCellUI();
                    cell.SetTextColor(Color.clear);
                    cell.SetPlayable(false);
                }
                // Column labels (A..)
                else if (y == 0)
                {
                    char letter = (char)('A' + x - 1);
                    cell.SetLetter(letter);
                    cell.SetTextColor(rowColor);
                    cell.DisableCellUI();
                    cell.SetPlayable(false);
                }
                // Row labels (1..)
                else if (x == 0)
                {
                    cell.SetLetterString(y.ToString());
                    cell.SetTextColor(ColumnColor);
                    cell.DisableCellUI();
                    cell.SetPlayable(false);
                }
                else
                {
                    cell.SetLetter(' ');
                    cell.SetTextColor(Color.black);
                    cell.SetPlayable(true);
                }

                grid[x, y] = cell;
            }
        }
    }

    private void PlaceFixedLettersFromSolution()
    {
        int sizeY = Mathf.Min(gridSize, solution.GetLength(0));
        int sizeX = Mathf.Min(gridSize, solution.GetLength(1));

        for (int y = 1; y < sizeY; y++)       // skip header row
        {
            for (int x = 1; x < sizeX; x++)   // skip header col
            {
                char c = solution[y, x];
                if (c != '\0')
                {
                    grid[x, y].SetLetter(c);
                }
            }
        }
    }

    private void FillRandomLetters()
    {
        System.Random rand = new System.Random(BuildBoardSeed());
        for (int y = 1; y < gridSize; y++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                if (grid[x, y].GetLetter() == ' ')
                    grid[x, y].SetLetter((char)('A' + rand.Next(26)));
            }
        }
    }

    private int BuildBoardSeed()
    {
        int seed = 17;
        string seedSource = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
            ? PhotonNetwork.CurrentRoom.Name
            : gameObject.scene.name;

        foreach (char c in seedSource)
            seed = (seed * 31) + c;

        seed = (seed * 31) + gridSize;
        return seed;
    }

    private void SyncCellState(LetterCell cell)
    {
        if (cell == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        Vector2Int pos = cell.gridPosition;
        int state = EncodeCellState(cell.IsSelected, cell.CurrentResultState);

        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
        {
            [BuildCellStateKey(pos.x, pos.y)] = state
        });

        object[] eventData =
        {
            pos.x,
            pos.y,
            state
        };

        PhotonNetwork.RaiseEvent(
            WordSearchCellStateEventCode,
            eventData,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendReliable);
    }

    private void ApplyRoomSyncedCellStates()
    {
        if (grid == null || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        suppressNetworkStateApply = true;

        for (int y = 1; y < gridSize; y++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                string key = BuildCellStateKey(x, y);
                if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(key))
                    continue;

                ApplyEncodedStateToCell(x, y, (int)PhotonNetwork.CurrentRoom.CustomProperties[key]);
            }
        }

        suppressNetworkStateApply = false;
    }

    private void ApplyEncodedStateToCell(int x, int y, int encodedState)
    {
        if (grid == null || x <= 0 || y <= 0 || x >= gridSize || y >= gridSize)
            return;

        LetterCell cell = grid[x, y];
        if (cell == null || !cell.IsPlayable)
            return;

        DecodeCellState(encodedState, out bool shouldSelect, out LetterCell.ResultVisualState resultState);
        ApplyCellState(cell, shouldSelect, resultState, false);
    }

    private void ApplyCellState(LetterCell cell, bool shouldSelect, LetterCell.ResultVisualState resultState, bool sync)
    {
        if (cell == null)
            return;

        if (shouldSelect) selected.Add(cell);
        else selected.Remove(cell);

        cell.ApplySharedState(shouldSelect, resultState);

        if (sync && !suppressNetworkStateApply)
            SyncCellState(cell);
    }

    private static int EncodeCellState(bool isSelected, LetterCell.ResultVisualState resultState)
    {
        return ((int)resultState << 1) | (isSelected ? 1 : 0);
    }

    private static void DecodeCellState(int encodedState, out bool isSelected, out LetterCell.ResultVisualState resultState)
    {
        isSelected = (encodedState & 1) == 1;
        resultState = (LetterCell.ResultVisualState)(encodedState >> 1);
    }

    private static string BuildCellStateKey(int x, int y)
    {
        return $"{WordSearchCellStatePrefix}{x}_{y}";
    }

    private void ClearSyncedCellStates()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return;

        Hashtable clearedStates = new Hashtable();

        for (int y = 1; y < gridSize; y++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                clearedStates[BuildCellStateKey(x, y)] = 0;
            }
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(clearedStates);
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != WordSearchCellStateEventCode)
            return;

        if (!(photonEvent.CustomData is object[] data) || data.Length < 3)
            return;

        suppressNetworkStateApply = true;
        ApplyEncodedStateToCell((int)data[0], (int)data[1], (int)data[2]);
        suppressNetworkStateApply = false;
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (grid == null)
            return;

        suppressNetworkStateApply = true;

        foreach (DictionaryEntry entry in propertiesThatChanged)
        {
            if (entry.Key is not string key || !key.StartsWith(WordSearchCellStatePrefix))
                continue;

            string[] parts = key.Substring(WordSearchCellStatePrefix.Length).Split('_');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int x) ||
                !int.TryParse(parts[1], out int y))
                continue;

            ApplyEncodedStateToCell(x, y, (int)entry.Value);
        }

        suppressNetworkStateApply = false;
    }

    // ---------- Results clearing ----------
    private void ClearAllResults(bool sync = true)
    {
        if (grid == null) return;
        int sizeY = grid.GetLength(1);
        int sizeX = grid.GetLength(0);

        for (int y = 1; y < sizeY; y++)
        {
            for (int x = 1; x < sizeX; x++)
            {
                var cell = grid[x, y];
                if (cell != null && cell.IsPlayable)
                    ApplyCellState(cell, selected.Contains(cell), LetterCell.ResultVisualState.None, sync);
            }
        }
    }

    // =========================
    //  Selection API for cells
    // =========================
    public void BeginDrag(LetterCell start)
    {
        if (!start.IsPlayable) return;

        isDragging = true;
        dragAddMode = !selected.Contains(start); // start on selected => unselect mode

        if (dragAddMode) SelectCell(start);
        else UnselectCell(start);
    }

    public void DragOver(LetterCell cell)
    {
        if (!isDragging || !cell.IsPlayable) return;

        if (dragAddMode) SelectCell(cell);
        else UnselectCell(cell);
    }

    public void EndDrag()
    {
        isDragging = false;
    }

    private void SelectCell(LetterCell cell)
    {
        if (!selected.Contains(cell) || cell.CurrentResultState != LetterCell.ResultVisualState.None)
            ApplyCellState(cell, true, LetterCell.ResultVisualState.None, true);
    }

    private void UnselectCell(LetterCell cell)
    {
        if (selected.Contains(cell) || cell.CurrentResultState != LetterCell.ResultVisualState.None)
            ApplyCellState(cell, false, LetterCell.ResultVisualState.None, true);
    }

    // Optional public single-click toggle (BeginDrag already handles it)
    public void ToggleCell(LetterCell cell)
    {
        if (!cell.IsPlayable) return;
        if (selected.Contains(cell)) UnselectCell(cell);
        else SelectCell(cell);
    }

    // Keep button wiring simple
    public void Submit() => IsSolved();

    // =========================
    // Final check + painter
    // =========================
    // True iff:
    //   1) every solution cell is selected AND matches its letter, and
    //   2) no extra cells are selected outside the solution map.
    // Also: clears previous results, then paints current selection green/red.
    public bool IsSolved()
    {
        ClearAllResults(); // fresh attempt paint

        int sizeY = Mathf.Min(gridSize, solution.GetLength(0));
        int sizeX = Mathf.Min(gridSize, solution.GetLength(1));

        bool allMatch = true;

        // 1) Paint every selected cell and track correctness
        foreach (var cell in selected)
        {
            Vector2Int p = cell.gridPosition;
            if (p.x <= 0 || p.y <= 0 || p.x >= sizeX || p.y >= sizeY) // ignore headers/out-of-range
                continue;

            char expected = solution[p.y, p.x];
            char actual = cell.GetLetter();

            bool isCorrect = (expected != '\0') &&
                             (char.ToUpperInvariant(expected) == char.ToUpperInvariant(actual));

            if (isCorrect) ApplyCellState(cell, true, LetterCell.ResultVisualState.Correct, true); // green
            else ApplyCellState(cell, true, LetterCell.ResultVisualState.Wrong, true);   // red

            if (!isCorrect) allMatch = false;
        }

        // 2) Ensure no required cell is missing, and that selected ones match
        for (int y = 1; y < sizeY; y++)
        {
            for (int x = 1; x < sizeX; x++)
            {
                if (solution[y, x] == '\0') continue;

                var neededCell = grid[x, y];

                // must be selected
                if (!selected.Contains(neededCell))
                {
                    allMatch = false;
                }
                else
                {
                    // and must match expected letter
                    if (char.ToUpperInvariant(neededCell.GetLetter()) !=
                        char.ToUpperInvariant(solution[y, x]))
                    {
                        allMatch = false;
                    }
                }
            }
        }

        return allMatch;
    }

    public void ResetPuzzle()
    {
        // --- 1) Destroy all existing grid children ---
        if (gridPanel == null)
        {
            Debug.LogError("[WordSearch] gridPanel is NULL");
            return;
        }

        Debug.Log($"[WordSearch] ResetPuzzle - children BEFORE = {gridPanel.transform.childCount}");

        for (int i = gridPanel.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = gridPanel.transform.GetChild(i);
            Destroy(child.gameObject);
        }

        // --- 2) Reset selection state ---
        selected.Clear();
        isDragging = false;
        suppressNetworkStateApply = false;
        ClearSyncedCellStates();

        grid = null;

        // --- 3) Regenerate everything ---
        GenerateGrid();
        PlaceFixedLettersFromSolution();
        FillRandomLetters();
        ApplyRoomSyncedCellStates();

        Debug.Log($"[WordSearch] ResetPuzzle - DONE, children AFTER = {gridPanel.transform.childCount}");
    }

}
