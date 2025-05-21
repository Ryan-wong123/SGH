using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CrosswordManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public Transform gridParent; // A GridLayoutGroup
    public int gridSize = 13;

    private CrosswordCell[,] grid;

    private char[,] solution = new char[13, 13]
    {
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
    };



    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        grid = new CrosswordCell[gridSize, gridSize];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                GameObject cellGO = Instantiate(cellPrefab, gridParent);
                CrosswordCell cell = cellGO.GetComponent<CrosswordCell>();

                int visualY = gridSize - 1 - y; // flip vertically to match Unity's layout
                cell.position = new Vector2Int(x, visualY);

                char letter = solution[y, x];
                bool active = letter != '\0';
                cell.SetInteractable(active);
                grid[y, x] = cell;
            }
        }
    }


    public bool CheckPuzzle()
    {
        bool correct = true;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                if (solution[y, x] == '\0') continue;

                char expected = solution[y, x];
                char typed = grid[y, x].GetLetter();

                if (char.ToUpper(typed) != char.ToUpper(expected))
                {
                    correct = false;
                    grid[y, x].inputField.image.color = Color.red;
                }
                else
                {
                    grid[y, x].inputField.image.color = Color.green;
                }
            }
        }

        return correct; // return result
    }

    public void ResetPuzzle()
    {
        foreach (var cell in grid) // assuming you have a 2D crossword cell array
        {
            if (cell != null)
            {
                cell.inputField.text = "";
                cell.inputField.image.color = Color.white;
            }
        }

        Debug.Log("Crossword puzzle reset.");
    }


}
