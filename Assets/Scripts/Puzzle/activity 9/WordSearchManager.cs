using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class WordSearchManager : MonoBehaviour, IPuzzle
{
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

    private void Start()
    {
        // If something calls Start twice / object re-enabled, don’t build again
        if (_built) return;

        // Always start clean (prevents any leftover children if prefab already contains them)
        ClearGridPanelImmediate();

        GenerateGrid();
        PlaceFixedLettersFromSolution();
        FillRandomLetters();

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
        System.Random rand = new System.Random();
        for (int y = 1; y < gridSize; y++)
        {
            for (int x = 1; x < gridSize; x++)
            {
                if (grid[x, y].GetLetter() == ' ')
                    grid[x, y].SetLetter((char)('A' + rand.Next(26)));
            }
        }
    }

    // ---------- Results clearing ----------
    private void ClearAllResults()
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
                    cell.ClearResult();
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
        if (selected.Add(cell))
        {
            cell.ClearResult();          // clear any prior green/red for iterative attempts
            cell.SetSelectedVisual(true);
        }
    }

    private void UnselectCell(LetterCell cell)
    {
        if (selected.Remove(cell))
        {
            cell.ClearResult();          // clear any prior green/red for iterative attempts
            cell.SetSelectedVisual(false);
        }
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

            if (isCorrect) cell.SetResultCorrect(); // green
            else cell.SetResultWrong();   // red

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

        grid = null;

        // --- 3) Regenerate everything ---
        GenerateGrid();
        PlaceFixedLettersFromSolution();
        FillRandomLetters();

        Debug.Log($"[WordSearch] ResetPuzzle - DONE, children AFTER = {gridPanel.transform.childCount}");
    }

}
