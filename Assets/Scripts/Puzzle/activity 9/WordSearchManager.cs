using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WordSearchManager : MonoBehaviour, IPuzzle
{
    public GameObject gridPanel;
    public GameObject cellPrefab;
    public int gridSize = 20;

    private LetterCell[,] grid;
    [SerializeField] private List<string> wordsToFind = new List<string>();
    private List<string> foundWords = new List<string>();
    private Dictionary<string, List<LetterCell>> wordToCells = new();

    private List<LetterCell> selectedCells = new List<LetterCell>(); // Current drag
    private HashSet<LetterCell> permanentSelectedCells = new();     // Across all drags
    private bool isSelecting = false;

    [SerializeField] private bool useFixedPlacement = false;

    private char[,] solution = new char[20, 20]
    {
        { 'S','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','L' ,'I' ,'N' ,'E' ,'Z' ,'O' ,'L' ,'I' ,'D'   },
        { 'E','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','\0','\0','S'  },
        { 'L','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','\0','\0','A'  },
        { 'O','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','\0','\0','E'  },
        { 'Z','\0','F' ,'L' ,'U' ,'O' ,'R' ,'O' ,'Q' ,'U' ,'I'  ,'N' ,'O' ,'L' ,'O' ,'N' ,'E' ,'S' ,'\0','R'  },
        { 'A','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','N','\0','U'  },
        { 'I' ,'M' ,'P' ,'A' ,'I' ,'R' ,'E' ,'D' ,'\0','\0','\0','\0','\0','\0','\0','\0','\0','O','\0','L'  },
        { '\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','I','\0','Y'  },
        { '\0','\0','\0','\0','\0','\0','D','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','T','\0','N'  },
        { '\0','\0','\0','\0','\0','\0','E','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','C','\0','O'  },
        { '\0','\0','\0','\0','\0','\0','R','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','U','\0','F'  },
        { '\0','\0','\0','\0','\0','\0','L','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','D','\0','L'  },
        { '\0','\0','\0','\0','\0','\0','Y','\0','\0','\0','\0' ,'\0','\0','\0','\0','\0','\0','E','\0','U'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','R','\0','S'  },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' },
        { '\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0','\0' }
    };

    void Start()
    {
        for (int i = 0; i < wordsToFind.Count; i++)
            wordsToFind[i] = wordsToFind[i].ToUpper();

        GenerateGrid();

        if (useFixedPlacement)
        {
            PlaceFixedWords();
        }
        else
        {
            PlaceWords();
        }

        FillRandomLetters();

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

                    // ✅ Highlight in green and lock
                    grid[x, y].Highlight(true, Color.grey);
       
                }
            }
        }
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

            // ✅ Highlight in green and lock
            grid[xi, yi].Highlight(true, Color.blue);
       
        }
        return true;
    }

    Vector2Int RandomDirection(System.Random rand)
    {
        // Only allow horizontal (→) and vertical (↓) directions
        Vector2Int[] directions = {
        new Vector2Int(1, 0),  // right
        new Vector2Int(0, 1),  // down
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
        selectedCells.Clear(); // Fresh drag
        UpdateSelection(start);
    }

    public void UpdateSelection(LetterCell current)
    {
        if (!isSelecting || current.isLocked) return;

        if (permanentSelectedCells.Contains(current) && !selectedCells.Contains(current))
        {
            // Unhighlight immediately if dragging over a previously selected cell
            permanentSelectedCells.Remove(current);
            current.Highlight(false);
        }
        else if (!selectedCells.Contains(current) && !permanentSelectedCells.Contains(current))
        {
            // Highlight new cell
            selectedCells.Add(current);
            current.Highlight(true);
        }
    }

    public void EndSelection()
    {
        isSelecting = false;

        List<LetterCell> finalSelection = new();
        foreach (var cell in selectedCells)
        {
            if (permanentSelectedCells.Contains(cell))
            {
                permanentSelectedCells.Remove(cell);
                cell.Highlight(false);
            }
            else
            {
                permanentSelectedCells.Add(cell);
                finalSelection.Add(cell);
            }
        }

        if (finalSelection.Count > 0)
        {
            string selectedWord = BuildOrderedWordFromSelection(finalSelection);


            string sortedSelected = SortString(selectedWord);

            foreach (string word in wordsToFind)
            {
                if (SortString(word) == sortedSelected && !foundWords.Contains(word))
                {
                    wordToCells[word] = new List<LetterCell>(finalSelection);
                    foundWords.Add(word);
                    break;
                }
            }
        }

        selectedCells.Clear();
    }
    public void ToggleSelection(LetterCell cell)
    {
        if (cell.isLocked) return;

        if (permanentSelectedCells.Contains(cell))
        {
            permanentSelectedCells.Remove(cell);
            cell.Highlight(false);
        }
        else
        {
            permanentSelectedCells.Add(cell);
            cell.Highlight(true);
        }

        CheckTappedWord();
    }
    private void CheckTappedWord()
    {
        string selectedWord = "";
        List<LetterCell> tempList = new();

        foreach (var cell in permanentSelectedCells)
        {
            selectedWord += cell.GetLetter();
            tempList.Add(cell);
        }

        // Optionally sort by position if you want (for anagram prevention)

        if (!string.IsNullOrEmpty(selectedWord))
        {
            if (wordsToFind.Contains(selectedWord) && !foundWords.Contains(selectedWord))
            {
                wordToCells[selectedWord] = new List<LetterCell>(tempList);
                foundWords.Add(selectedWord);

                foreach (var c in tempList)
                {
                    c.Highlight(true, Color.green);
                    c.isLocked = true;
                }

                permanentSelectedCells.Clear(); // Clear for next word
            }
            else if (selectedWord.Length >= 3) // optional: auto reset if wrong
            {
                foreach (var c in tempList)
                    c.Highlight(false);
                permanentSelectedCells.Clear();
            }
        }
    }
    private string BuildOrderedWordFromSelection(List<LetterCell> cells)
    {
        if (cells.Count < 2)
            return "";

        // Get direction from first two cells
        Vector2Int dir = cells[1].gridPosition - cells[0].gridPosition;

        if (dir == Vector2Int.zero) return "";

        List<LetterCell> ordered = new() { cells[0] };
        Vector2Int currentPos = cells[0].gridPosition;

        for (int i = 1; i < cells.Count; i++)
        {
            currentPos += dir;
            LetterCell next = cells.Find(c => c.gridPosition == currentPos);
            if (next == null) return ""; // Broken sequence
            ordered.Add(next);
        }

        string word = "";
        foreach (var cell in ordered)
            word += cell.GetLetter();

        return word;
    }

    private string SortString(string input)
    {
        char[] chars = input.ToCharArray();
        System.Array.Sort(chars);
        return new string(chars);
    }


    private List<LetterCell> GetCellsFromWord(string word)
    {
        return wordToCells.TryGetValue(word, out var cells) ? cells : new List<LetterCell>();
    }

    private bool AllWordsFound()
    {
        Debug.Log("Checking all words...");

        bool allCorrect = true;

        foreach (string word in foundWords)
        {
            bool isValid = wordsToFind.Contains(word);

            Debug.Log($"{word} is valid {isValid}");

            foreach (LetterCell cell in GetCellsFromWord(word))
            {
                if (isValid)
                {
                    cell.Highlight(true, Color.green);
                    cell.isLocked = true;
                }
                else
                {
                    cell.Highlight(false);
                    allCorrect = false;
                }
            }
        }
        Debug.Log($"all correct {allCorrect} foundwords count {foundWords.Count} wordstofind count {wordsToFind.Count} ");
        return allCorrect && foundWords.Count == wordsToFind.Count;
    }

    public bool IsSolved()
    {
        return AllWordsFound();
    }
}
