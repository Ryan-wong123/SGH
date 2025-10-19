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

    private List<LetterCell> selectedCells = new();
    private HashSet<LetterCell> permanentSelectedCells = new();
    private bool isSelecting = false;

    [SerializeField] private bool useFixedPlacement = false;

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

    void Start()
    {
        for (int i = 0; i < wordsToFind.Count; i++)
            wordsToFind[i] = wordsToFind[i].ToUpper();

        GenerateGrid();

        if (useFixedPlacement)
            PlaceFixedWords();
        else
            PlaceWords();

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
            grid[xi, yi].Highlight(true, Color.blue);
        }

        return true;
    }

    Vector2Int RandomDirection(System.Random rand)
    {
        return new Vector2Int[] { new(1, 0), new(0, 1) }[rand.Next(2)];
    }

    void FillRandomLetters()
    {
        System.Random rand = new System.Random();
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (grid[x, y].GetLetter() == ' ')
                    grid[x, y].SetLetter((char)('A' + rand.Next(26)));
            }
        }
    }

    public void StartSelection(LetterCell start)
    {
        isSelecting = true;
        selectedCells.Clear();
        UpdateSelection(start);
    }

    public void UpdateSelection(LetterCell current)
    {
        if (!isSelecting) return;

        if (!selectedCells.Contains(current))
        {
            selectedCells.Add(current);
            if (!current.isLocked)
                current.Highlight(true);
        }
    }

    public void EndSelection()
    {
        isSelecting = false;

        foreach (var cell in selectedCells)
        {
            if (!permanentSelectedCells.Contains(cell))
            {
                permanentSelectedCells.Add(cell);
                if (!cell.isLocked)
                    cell.Highlight(true);
            }
        }

        selectedCells.Clear();
        CheckTappedWord();
    }

    public void ToggleSelection(LetterCell cell)
    {
        if (permanentSelectedCells.Contains(cell))
        {
            permanentSelectedCells.Remove(cell);
            if (!cell.isLocked)
                cell.Highlight(false);
        }
        else
        {
            permanentSelectedCells.Add(cell);
            if (!cell.isLocked)
                cell.Highlight(true);
        }

        CheckTappedWord();
    }

    private void CheckTappedWord()
    {
        string selectedWord = "";
        List<LetterCell> tempList = new(permanentSelectedCells);

        foreach (var cell in tempList)
            selectedWord += cell.GetLetter();

        foreach (string word in wordsToFind)
        {
            if (selectedWord.Length != word.Length) continue;

            if (IsLetterSetEqual(selectedWord, word) && !foundWords.Contains(word))
            {
                wordToCells[word] = new List<LetterCell>(tempList);
                foundWords.Add(word);

                // ✅ Do NOT highlight/lock here
                permanentSelectedCells.Clear();
                return;
            }
        }

        // Word invalid: reset highlights
        foreach (var c in tempList)
        {
            if (!c.isLocked)
                c.Highlight(false);
        }

        permanentSelectedCells.Clear();
    }

    public void SubmitWords()
    {
        foreach (string word in foundWords)
        {
            bool isValid = wordsToFind.Contains(word);
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
                }
            }
        }
    }

    private bool IsLetterSetEqual(string a, string b)
    {
        var aArr = a.ToCharArray();
        var bArr = b.ToCharArray();
        System.Array.Sort(aArr);
        System.Array.Sort(bArr);
        return new string(aArr) == new string(bArr);
    }

    public bool IsSolved() => AllWordsFound();

    private bool AllWordsFound()
    {
        bool allCorrect = true;
        foreach (string word in foundWords)
        {
            bool isValid = wordsToFind.Contains(word);
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

        return allCorrect && foundWords.Count == wordsToFind.Count;
    }

    private List<LetterCell> GetCellsFromWord(string word)
    {
        return wordToCells.TryGetValue(word, out var cells) ? cells : new List<LetterCell>();
    }
}
