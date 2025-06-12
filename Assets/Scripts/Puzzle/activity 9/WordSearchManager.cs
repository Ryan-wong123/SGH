using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class WordSearchManager : MonoBehaviour, IPuzzle
{
    public GameObject gridPanel;
    public GameObject cellPrefab;
    public int gridSize = 15;

    private LetterCell[,] grid;
    [SerializeField] private List<string> wordsToFind = new List<string>();
    private List<string> foundWords = new List<string>();
    private List<LetterCell> selectedCells = new List<LetterCell>();
    private bool isSelecting = false;

    void Start()
    {
        GenerateGrid();
        PlaceWords();
        FillRandomLetters();
    }

    void GenerateGrid()
    {
        grid = new LetterCell[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                GameObject obj = Instantiate(cellPrefab, gridPanel.transform);
                LetterCell cell = obj.GetComponent<LetterCell>();
                cell.gridPosition = new Vector2Int(x, y);
                cell.SetLetter(' ');
                grid[x, y] = cell;
            }
        }
    }

    void PlaceWords()
    {
        System.Random rand = new System.Random();
        foreach (string word in wordsToFind)
        {
            bool placed = false;
            for (int attempts = 0; attempts < 100 && !placed; attempts++)
            {
                int x = rand.Next(gridSize);
                int y = rand.Next(gridSize);
                Vector2Int dir = RandomDirection(rand);
                placed = TryPlaceWord(word.ToUpper(), x, y, dir);
            }
        }
    }

    bool TryPlaceWord(string word, int x, int y, Vector2Int dir)
    {
        int endX = x + dir.x * (word.Length - 1);
        int endY = y + dir.y * (word.Length - 1);

        if (endX < 0 || endY < 0 || endX >= gridSize || endY >= gridSize)
            return false;

        for (int i = 0; i < word.Length; i++)
        {
            int xi = x + i * dir.x;
            int yi = y + i * dir.y;
            char existing = grid[xi, yi].GetLetter();
            if (existing != ' ' && existing != word[i])
                return false;
        }

        for (int i = 0; i < word.Length; i++)
        {
            int xi = x + i * dir.x;
            int yi = y + i * dir.y;
            grid[xi, yi].SetLetter(word[i]);
        }
        return true;
    }

    Vector2Int RandomDirection(System.Random rand)
    {
        Vector2Int[] directions = {
            new Vector2Int(1, 0), new Vector2Int(0, 1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, -1),
            new Vector2Int(-1, -1), new Vector2Int(1, -1)
        };
        return directions[rand.Next(directions.Length)];
    }

    void FillRandomLetters()
    {
        System.Random rand = new System.Random();
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (grid[x, y].GetLetter() == ' ')
                {
                    char c = (char)('A' + rand.Next(26));
                    grid[x, y].SetLetter(c);
                }
            }
        }
    }

    public void StartSelection(LetterCell start)
    {
        isSelecting = true;
        UpdateSelection(start);
    }

    public void UpdateSelection(LetterCell current)
    {
        if (!isSelecting || current.isLocked) return;

        if (selectedCells.Contains(current))
        {
            selectedCells.Remove(current);
            current.Highlight(false);
        }
        else
        {
            selectedCells.Add(current);
            current.Highlight(true);
        }
    }

    public void EndSelection()
    {
        isSelecting = false;

        //foreach (LetterCell c in selectedCells)
        //    c.Highlight(true); // temporary highlight only

        // Do not lock cells — final check will validate and lock them
    }

    private bool MatchesWordFromCell(LetterCell start, string word, Vector2Int dir)
    {
        int x = start.gridPosition.x;
        int y = start.gridPosition.y;

        for (int i = 0; i < word.Length; i++)
        {
            int xi = x + dir.x * i;
            int yi = y + dir.y * i;

            if (xi < 0 || yi < 0 || xi >= gridSize || yi >= gridSize)
                return false;

            LetterCell cell = grid[xi, yi];

            if (!cell.isLocked || cell.GetLetter() != word[i])
                return false;
        }

        int endX = x + dir.x * word.Length;
        int endY = y + dir.y * word.Length;

        if (endX >= 0 && endY >= 0 && endX < gridSize && endY < gridSize)
        {
            if (grid[endX, endY].isLocked)
                return false;
        }

        return true;
    }

    private bool IsWordInLockedCells(string word)
    {
        word = word.ToUpper();

        foreach (LetterCell cell in grid)
        {
            if (!cell.isLocked) continue;

            foreach (Vector2Int dir in new Vector2Int[] {
                new Vector2Int(1, 0), new Vector2Int(0, 1),
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(-1, 0), new Vector2Int(0, -1),
                new Vector2Int(-1, -1), new Vector2Int(1, -1)
            })
            {
                if (MatchesWordFromCell(cell, word, dir))
                    return true;
            }
        }

        return false;
    }

    private void ApplyColorFeedback()
    {
        foreach (LetterCell cell in grid)
        {
            if (!cell.isLocked) continue;

            bool isCorrect = false;

            foreach (string word in foundWords)
            {
                if (IsWordInLockedCells(word))
                {
                    isCorrect = true;
                    break;
                }
            }

            if (isCorrect)
            {
                cell.isLocked = true;
                cell.Highlight(true, Color.green);
            }
            else
            {
                cell.Highlight(true, Color.red);
            }
        }
    }

    private bool AllWordsFound()
    {
        Debug.Log($"Checking all words...");

        foundWords.Clear();

        foreach (string word in wordsToFind)
        {
            if (IsWordInLockedCells(word))
            {
                foundWords.Add(word);
            }
        }

        Debug.Log($"Found {foundWords.Count} of {wordsToFind.Count}");

        ApplyColorFeedback();

        return foundWords.Count == wordsToFind.Count;
    }

    public bool IsSolved()
    {
        return AllWordsFound();
    }
}
