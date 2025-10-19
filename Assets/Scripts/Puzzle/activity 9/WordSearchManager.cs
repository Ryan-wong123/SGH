using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class WordSearchManager : MonoBehaviour, IPuzzle
{
    [Header("Grid")]
    public GameObject gridPanel;
    public GameObject cellPrefab;
    public int gridSize = 20; // includes headers row/col 0

    private LetterCell[,] grid;

    [Header("Words")]
    [SerializeField] private List<string> wordsToFind = new();
    private HashSet<string> wordsUpper; // cached

    [Header("Placement")]
    [SerializeField] private bool useFixedPlacement = false;

    // Optional fixed puzzle (your original)
    private char[,] solution = new char[21, 21]
 {
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L' ,'I' ,'N' ,'E' ,'Z' ,'O' ,'L' ,'I' ,'D'   },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S'  },
    { '\0','\0','\0','\0','\0','\0','R' ,'E' ,'D' ,'U' ,'C' ,'T' ,'I' ,'O' ,'N' ,'F','\0','\0','\0','\0','U'  },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','L'  },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','U','\0','\0','\0','\0','F'  },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','O','\0','\0','\0','\0','O'  },
    { '\0','\0','\0','\0','\0','\0','\0','\0', '\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','N'  },
    { '\0','A','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','O','\0','\0','\0','\0','Y'  },
    { '\0','Z','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','Q','\0','\0','\0','\0','L'  },
    { '\0','O','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','U','\0','\0','\0','\0','U'  },
    { '\0','L','\0','\0','\0','\0','\0','E','\0','\0','\0','\0' ,'\0','\0','\0','I','\0','\0','\0','\0','R'  },
    { '\0','E','\0','\0','\0','\0','\0','L','\0','\0','\0','\0' ,'\0','\0','\0','N','\0','\0','\0','\0','E'  },
    { '\0','S','\0','\0','\0','\0','\0','D','\0','\0','\0','\0' ,'\0','\0','\0','O','\0','\0','\0','\0','A'  },
    { '\0','I','M' ,'P' ,'A' ,'I' ,'R' ,'E' ,'D','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','S'  },
    { '\0','\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0','\0','\0','\0','O','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0','\0','\0','\0','N','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','Y','\0','\0','\0','\0','\0','\0','\0','E','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','S','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
    { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' }
 };

    // ===== Selection model =====
    private class Selection
    {
        public List<LetterCell> Cells = new();
        public string WordForward => new string(Cells.Select(c => c.GetLetter()).ToArray());
        public string WordReverse
            => new string(Cells.AsEnumerable().Reverse().Select(c => c.GetLetter()).ToArray());
    }

    private readonly List<Selection> userSelections = new();     // stored, editable until Submit
    private Selection preview = null;                             // live drag/2-click preview
    private LetterCell dragStart = null;                          // first cell in drag/manual
    private bool isDragging = false;

    // ===== Unity =====
    void Start()
    {
        for (int i = 0; i < wordsToFind.Count; i++) wordsToFind[i] = wordsToFind[i].ToUpper();
        wordsUpper = new HashSet<string>(wordsToFind);

        GenerateGrid();

        if (useFixedPlacement) PlaceFixedWords();
        else PlaceWordsRandomly();

        FillRandomLetters();
    }

    // ===== Grid build and placement =====
    void GenerateGrid()
    {
        grid = new LetterCell[gridSize, gridSize];

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var obj = Instantiate(cellPrefab, gridPanel.transform);
                var cell = obj.GetComponent<LetterCell>();
                cell.gridPosition = new Vector2Int(x, y);

                if (x == 0 && y == 0)
                {
                    cell.SetLetter(' ');
                    cell.DisableCellUI();
                    cell.SetTextColor(Color.clear);
                }
                else if (y == 0)
                {
                    char letter = (char)('A' + x - 1);
                    cell.SetLetter(letter);
                    cell.SetTextColor(Color.white);
                    cell.DisableCellUI();
                }
                else if (x == 0)
                {
                    cell.SetLetterString(y.ToString());
                    cell.SetTextColor(Color.white);
                    cell.DisableCellUI();
                }
                else
                {
                    cell.SetLetter(' ');
                    cell.SetTextColor(Color.black);
                }

                grid[x, y] = cell;
            }
        }
    }

    private void PlaceFixedWords()
    {
        int size = Mathf.Min(gridSize, solution.GetLength(0));
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                char c = solution[y, x];
                if (c != '\0')
                {
                    grid[x, y].SetLetter(c);
                    // grey to visualize fixed slots
                    grid[x, y].Highlight(true, Color.grey);
                }
            }
        }
    }

    private void PlaceWordsRandomly()
    {
        System.Random rand = new System.Random();
        Vector2Int[] dirs = {
            new(1,0), new(0,1), new(1,1), new(1,-1),
            new(-1,0), new(0,-1), new(-1,-1), new(-1,1)
        };

        foreach (string word in wordsUpper)
        {
            bool placed = false;
            for (int attempts = 0; attempts < 200 && !placed; attempts++)
            {
                int x = rand.Next(1, gridSize); // skip header 0
                int y = rand.Next(1, gridSize);
                var dir = dirs[rand.Next(dirs.Length)];
                placed = TryPlaceWord(word, x, y, dir);
            }
        }
    }

    private bool TryPlaceWord(string word, int x, int y, Vector2Int dir)
    {
        int endX = x + dir.x * (word.Length - 1);
        int endY = y + dir.y * (word.Length - 1);
        if (endX <= 0 || endY <= 0 || endX >= gridSize || endY >= gridSize) return false;

        for (int i = 0; i < word.Length; i++)
        {
            int xi = x + i * dir.x;
            int yi = y + i * dir.y;
            char existing = grid[xi, yi].GetLetter();
            if (existing != ' ' && existing != word[i]) return false;
        }

        for (int i = 0; i < word.Length; i++)
        {
            int xi = x + i * dir.x;
            int yi = y + i * dir.y;
            grid[xi, yi].SetLetter(word[i]);
        }
        return true;
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

    // ===== Pointer workflow =====
    public void BeginPointer(LetterCell start)
    {
        if (!IsPlayableCell(start)) return;

        dragStart = start;
        isDragging = true;
        StartPreview(start);
    }

    public void DragPreviewTo(LetterCell current)
    {
        if (!isDragging || dragStart == null) return;
        if (!IsPlayableCell(current)) return;

        UpdatePreviewPath(dragStart, current);
    }

    public void EndPointer(LetterCell end)
    {
        if (!IsPlayableCell(end)) { ClearPreview(); isDragging = false; dragStart = null; return; }

        if (isDragging && dragStart != null)
        {
            FinalizePreviewToSelection(); // drag mode
        }
        else
        {
            // Manual 2-click mode: if no preview yet, start; else finalize
            if (preview == null) StartPreview(end);
            else
            {
                UpdatePreviewPath(preview.Cells.First(), end);
                FinalizePreviewToSelection();
            }
        }

        isDragging = false;
        dragStart = null;
    }

    // Right-click remove
    public void TryRemoveSelectionContaining(LetterCell anyCell)
    {
        var sel = userSelections.FirstOrDefault(s => s.Cells.Contains(anyCell));
        if (sel != null)
        {
            foreach (var c in sel.Cells) if (!c.isLocked) c.Highlight(false);
            userSelections.Remove(sel);
        }
    }

    // ===== Preview helpers =====
    private void StartPreview(LetterCell start)
    {
        ClearPreview();
        preview = new Selection();
        preview.Cells.Add(start);
        if (!start.isLocked) start.Highlight(true, Color.yellow);
    }

    private void UpdatePreviewPath(LetterCell start, LetterCell end)
    {
        // Clear previous preview highlight (but not stored selections)
        if (preview != null)
        {
            foreach (var c in preview.Cells) if (!c.isLocked) c.Highlight(false);
        }

        var path = ComputeStraightLine(start.gridPosition, end.gridPosition);
        preview = new Selection();
        foreach (var p in path)
        {
            var c = grid[p.x, p.y];
            preview.Cells.Add(c);
            if (!c.isLocked) c.Highlight(true, Color.yellow);
        }
    }

    private void FinalizePreviewToSelection()
    {
        if (preview == null || preview.Cells.Count < 2)
        {
            ClearPreview();
            return;
        }

        // Paint stored (editable) selections blue
        foreach (var c in preview.Cells) if (!c.isLocked) c.Highlight(true, Color.blue);

        userSelections.Add(preview);
        preview = null;
    }

    private void ClearPreview()
    {
        if (preview == null) return;
        foreach (var c in preview.Cells) if (!c.isLocked) c.Highlight(false);
        preview = null;
    }

    private bool IsPlayableCell(LetterCell c) => c.gridPosition.x > 0 && c.gridPosition.y > 0;

    // 8-direction Bresenham-lite for straight lines
    private List<Vector2Int> ComputeStraightLine(Vector2Int a, Vector2Int b)
    {
        List<Vector2Int> points = new();

        int dx = b.x - a.x;
        int dy = b.y - a.y;

        int stepX = dx == 0 ? 0 : (dx > 0 ? 1 : -1);
        int stepY = dy == 0 ? 0 : (dy > 0 ? 1 : -1);

        // Must be straight (horizontal/vertical) or perfect diagonal
        if (!(dx == 0 || dy == 0 || Mathf.Abs(dx) == Mathf.Abs(dy)))
        {
            // Not straight: snap to the closest straight candidate among 8 dirs
            // Keep just the start cell so user sees they need a straight line
            points.Add(a);
            return points;
        }

        int len = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) + 1;
        int x = a.x, y = a.y;
        for (int i = 0; i < len; i++)
        {
            if (x <= 0 || y <= 0 || x >= gridSize || y >= gridSize) break;
            points.Add(new Vector2Int(x, y));
            x += stepX;
            y += stepY;
        }

        return points;
    }

    // ===== Submission & puzzle API =====
    public void SubmitWords()
    {
        // Grade each stored selection once; lock the result color
        var matched = new HashSet<string>(); // avoid claiming same word twice (optional)

        foreach (var sel in userSelections)
        {
            string fwd = sel.WordForward.ToUpper();
            string rev = sel.WordReverse.ToUpper();

            bool hit = false;
            string hitWord = null;

            if (wordsUpper.Contains(fwd)) { hit = true; hitWord = fwd; }
            else if (wordsUpper.Contains(rev)) { hit = true; hitWord = rev; }

            // If we want each word only claimed once:
            if (hit && matched.Contains(hitWord)) hit = false;

            foreach (var c in sel.Cells)
            {
                if (hit)
                {
                    c.Highlight(true, Color.green);
                    c.isLocked = true;
                }
                else
                {
                    c.Highlight(true, Color.red);
                    c.isLocked = true;
                }
            }

            if (hit && hitWord != null) matched.Add(hitWord);
        }
    }

    public bool IsSolved()
    {
        // Consider solved when all required words have at least one green selection
        var greens = new HashSet<string>();
        foreach (var sel in userSelections)
        {
            string fwd = sel.WordForward.ToUpper();
            string rev = sel.WordReverse.ToUpper();
            if (wordsUpper.Contains(fwd)) greens.Add(fwd);
            if (wordsUpper.Contains(rev)) greens.Add(rev);
        }
        return greens.Count == wordsUpper.Count;
    }
}
