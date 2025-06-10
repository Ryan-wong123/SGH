using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class WordSearchManager : MonoBehaviour
{
    public GameObject gridPanel;
    public GameObject cellPrefab;
    public int gridSize = 15;

    private LetterCell[,] grid;
    private List<string> wordsToFind = new List<string>();
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

        // Do NOT clear — let UpdateSelection handle toggling
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

        string word = "";
        foreach (LetterCell c in selectedCells)
            word += c.GetLetter();

        if (CheckWord(word))
        {
            foreach (LetterCell c in selectedCells)
            {
                c.isLocked = true;
                c.Highlight(true);  // make sure it stays highlighted
            }

            if (AllWordsFound())
                Debug.Log("win");

            selectedCells.Clear(); // Clear selection for correct words
        }
        // ❌ NO ELSE BLOCK — don't clear or unhighlight incorrect selections
    }


    private void AddToSelection(LetterCell cell)
    {
        selectedCells.Add(cell);
        cell.Highlight(true);
    }


    private bool CheckWord(string selected)
    {
        if (wordsToFind.Contains(selected) && !foundWords.Contains(selected))
        {
            foundWords.Add(selected);
            return true;
        }
        return false;
    }

    private bool AllWordsFound()
    {
        return foundWords.Count == wordsToFind.Count;
    }
}
